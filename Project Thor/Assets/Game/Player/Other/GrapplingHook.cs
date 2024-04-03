using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GrapplingHook : BaseProjectile
{
    public PlayerMovement OwningPlayerMovement;
    public LineRenderer grapple;

    [HideInInspector]
    public bool bHit;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if(!IsServer)
        {
            TryFindOwner();
        }
    }

    private void TryFindOwner()
    {
        PlayerMovement[] playermovements = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (PlayerMovement i in playermovements)
        {
            if (i.GetOwnerID() == OwnerClientId)
            {
                i.GrapplePool.pooledObjects.Add(gameObject);
                i.GrapplePool.pooledNetworkObjects.Add(this);

                if(!IsOwner)
                {
                    OwningPlayerMovement = i;
                }

                return;
            }
        }

        Invoke(nameof(TryFindOwner), 1);
    }

    public override void OnHitGround()
    {
        if(IsServer)
        {
            OwningPlayerMovement.StartGrapple(SelfTransform.position);

            bHit = true;

            Invoke(nameof(Despawn), 0.5f);
            ReplicateHitClientRpc();
        }
    }

    public override void OnHitPlayerWithTarget(PlayerManager player)
    {
        if (IsServer)
        {
            if(player.GetIsHoldingBall())
            {
                Ball.Singleton.Attach(OwningPlayerMovement.GetPlayer());

                return;
            }

            OwningPlayerMovement.StartGrapple(SelfTransform.position);

            bHit = true;

            Invoke(nameof(Despawn), 0.5f);
            ReplicateHitClientRpc();
        }
    }

    public override void Activate()
    {
        base.Activate();

        grapple.enabled = true;
    }

    public override void Deactivate()
    {
        base.Deactivate();

        bHit = false;

        grapple.enabled = false;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateHitClientRpc()
    {
        if (IsServer)
        {
            return;
        }

        bHit = true;

        Invoke(nameof(Despawn), 0.5f);
    }
}
