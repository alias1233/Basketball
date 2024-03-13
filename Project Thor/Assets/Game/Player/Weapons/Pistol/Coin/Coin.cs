using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Coin : BaseProjectile
{
    [Header("Coin")]

    public float CoinRange;

    public LineRenderer Ricochet;

    public AudioSource ShootCoinSound;

    Collider[] Hits = new Collider[5];

    public override void OnHitGround()
    {
        ReplicateDisableClientRpc();
        gameObject.SetActive(false);
    }

    public void OnShoot()
    {
        int NumHits = Physics.OverlapSphereNonAlloc(SelfTransform.position, CoinRange, Hits, PlayerLayer);

        for (int i = 0; i < NumHits; i++)
        {
            PlayerManager player = Hits[i].GetComponent<PlayerManager>();

            if (player.GetTeam() != OwningPlayerTeam)
            {
                Vector3 PlayerPos = Hits[i].transform.position;
                player.Damage(OwningPlayerTeam, Damage);

                ReplicateShotPlayerClientRpc(PlayerPos);
                Invoke(nameof(DisableGameObject), 0.5f);
                Model.SetActive(false);

                Ricochet.SetPosition(0, SelfTransform.position);
                Ricochet.SetPosition(1, PlayerPos);
                Ricochet.enabled = true;
                Invoke(nameof(DisableRicochet), 0.5f);

                return;
            }
        }

        ReplicateShotClientRpc();
        gameObject.SetActive(false);
    }

    private void DisableRicochet()
    {
        Ricochet.enabled = false;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void ReplicateShotPlayerClientRpc(Vector3 hitplayerpos)
    {
        if(IsServer)
        {
            return;
        }

        Ricochet.SetPosition(0, SelfTransform.position);
        Ricochet.SetPosition(1, hitplayerpos);
        Ricochet.enabled = true;
        Invoke(nameof(DisableRicochet), 0.5f);

        Model.SetActive(false);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void ReplicateShotClientRpc()
    {
        if (IsServer)
        {
            return;
        }

        Model.SetActive(false);
    }
}
