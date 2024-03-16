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

    public override void Activate()
    {
        base.Activate();

        Invoke(nameof(ActivateCoinTrail), 0.01f);
    }

    public override void Deactivate()
    {
        base.Deactivate();

        CoinTrail.emitting = false;
    }

    private void ActivateCoinTrail()
    {
        CoinTrail.emitting = true;
    }

    public override void OnHitGround()
    {
        ReplicateDisableClientRpc();
        Despawn();
    }

    public void CoinInit(Teams team, Vector3 pos, Vector3 dir, Vector3 initalvel)
    {
        LastTimeReplicatedPosition = Time.time;
        CoinInitClientRpc(team, pos, dir, initalvel);
        StartTime = TimeStamp;

        OwningPlayerTeam = team;
        SelfTransform.position = pos;
        Velocity = dir * InitialSpeed + initalvel;
        SelfTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);
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
                Invoke(nameof(Despawn), 0.5f);
                CoinTrail.emitting = false;

                Ricochet.SetPosition(0, SelfTransform.position);
                Ricochet.SetPosition(1, PlayerPos);
                Ricochet.enabled = true;
                Invoke(nameof(DisableRicochet), 0.5f);

                return;
            }
        }

        ReplicateShotClientRpc();
        Despawn();
    }

    private void DisableRicochet()
    {
        Ricochet.enabled = false;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void CoinInitClientRpc(Teams team, Vector3 pos, Vector3 dir, Vector3 initialvel)
    {
        if (IsServer)
        {
            return;
        }

        if (!bIsActive)
        {
            Spawn();
        }

        bUpdatedThisFrame = true;

        StartTime = TimeStamp;

        OwningPlayerTeam = team;
        SelfTransform.position = pos;
        Velocity = dir * InitialSpeed + initialvel;
        SelfTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void ReplicateShotPlayerClientRpc(Vector3 hitplayerpos)
    {
        if(IsServer)
        {
            return;
        }

        Despawn();

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

        Despawn();
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void OnShootServerRpc()
    {
        OnShoot();
    }
}
