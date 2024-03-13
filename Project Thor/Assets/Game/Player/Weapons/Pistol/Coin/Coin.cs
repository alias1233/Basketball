using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Coin : BaseProjectile
{
    [Header("Coin")]

    public LineRenderer Ricochet;
    public TrailRenderer CoinTrail;
    public AudioSource ShootCoinSound;

    public float CoinRange;

    Collider[] Hits = new Collider[5];

    public override void DisableGameObject()
    {
        base.DisableGameObject();

        CoinTrail.emitting = false;
    }
    public override void EnableModel()
    {
        base.EnableModel();

        Invoke(nameof(ActivateCoinTrail), 0.1f);
    }

    public override void DisableModel()
    {
        base.DisableModel();

        CoinTrail.emitting = false;
    }

    private void ActivateCoinTrail()
    {
        CoinTrail.emitting = true;
    }

    public override void OnHitGround()
    {
        ReplicateDisableClientRpc();
        DisableGameObject();
    }

    public void OnShoot()
    {
        int NumHits = Physics.OverlapSphereNonAlloc(SelfTransform.position, CoinRange, Hits, PlayerLayer);

        for (int i = 0; i < NumHits; i++)
        {
            PlayerManager player = Hits[i].GetComponent<PlayerManager>();

            if (player.GetTeam() != OwningPlayerTeam)
            {
                Model.SetActive(false);

                Vector3 PlayerPos = Hits[i].transform.position;
                player.Damage(OwningPlayerTeam, Damage);

                ReplicateShotPlayerClientRpc(PlayerPos);
                Invoke(nameof(DisableGameObject), 0.5f);
                CoinTrail.emitting = false;

                Ricochet.SetPosition(0, SelfTransform.position);
                Ricochet.SetPosition(1, PlayerPos);
                Ricochet.enabled = true;
                Invoke(nameof(DisableRicochet), 0.5f);

                return;
            }
        }

        ReplicateShotClientRpc();
        DisableGameObject();
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

        DisableModel();

        Ricochet.SetPosition(0, SelfTransform.position);
        Ricochet.SetPosition(1, hitplayerpos);
        Ricochet.enabled = true;
        Invoke(nameof(DisableRicochet), 0.5f);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void ReplicateShotClientRpc()
    {
        if (IsServer)
        {
            return;
        }

        DisableModel();
    }
}
