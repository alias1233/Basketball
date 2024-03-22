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

        Invoke(nameof(TryFindOwnerAgain), 2);
    }

    private void TryFindOwnerAgain()
    {
        PlayerMovement[] playermovements = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (PlayerMovement i in playermovements)
        {
            if (i.GetOwnerID() == OwnerClientId)
            {
                OwningPlayerMovement = i;

                if (IsOwner && !IsServer)
                {
                    i.GrapplePool.pooledObjects.Add(gameObject);
                    i.GrapplePool.pooledNetworkObjects.Add(this);
                }

                return;
            }
        }

        Invoke(nameof(TryFindOwnerAgain), 1);
    }

    public override void OnHitGround()
    {
        if(IsServer)
        {
            OwningPlayerMovement.StartGrapple(SelfTransform.position);

            bHit = true;

            Invoke(nameof(DisableGrapple), 0.5f);

            ReplicateHitClientRpc();
            Despawn();
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

            Invoke(nameof(DisableGrapple), 0.5f);

            ReplicateHitClientRpc();
            Despawn();
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

        if(!bHit)
        {
            grapple.enabled = false;
        }
    }

    private void DisableGrapple()
    {
        grapple.enabled = false;

        bHit = false;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateHitClientRpc()
    {
        if (IsServer)
        {
            return;
        }

        bHit = true;

        Invoke(nameof(DisableGrapple), 0.5f);

        Despawn();
    }
}
