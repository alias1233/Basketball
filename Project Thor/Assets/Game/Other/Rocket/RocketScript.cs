using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RocketScript : BaseProjectile
{
    [SerializeField]
    ParticleSystem Explosion;

    public float Radius;

    Collider[] Hits = new Collider[5];

    public override void OnHitGround()
    {
        ExplodeClientRpc();
        Explode();
    }

    public override void OnHitPlayer()
    {
        ExplodeClientRpc();
        Explode();
    }

    private void Explode()
    {
        ExplodeVisuals();
        Model.SetActive(false);
        Invoke(nameof(DisableGameObject), 0.5f);

        int NumHits = Physics.OverlapSphereNonAlloc(transform.position, Radius, Hits, PlayerLayer);

        for(int i = 0; i < NumHits; i++)
        {
            Hits[i].GetComponent<PlayerManager>().Damage(OwningPlayerTeam, Mathf.Clamp(Damage * (1 - (Hits[i].transform.position - transform.position).magnitude / Radius), 0, Damage));
        }
    }

    private void ExplodeVisuals()
    {
        Explosion.Play();
    }

    [ClientRpc]
    private void ExplodeClientRpc()
    {
        if (IsServer)
        {
            return;
        }

        ExplodeVisuals();
        Model.SetActive(false);
    }
}
