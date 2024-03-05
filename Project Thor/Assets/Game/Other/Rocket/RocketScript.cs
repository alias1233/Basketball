using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RocketScript : BaseProjectile
{
    [SerializeField]
    ParticleSystem Explosion;

    public float Radius;

    public float Impulse;

    public Vector3 Offset;

    Collider[] Hits = new Collider[5];

    public override void OnHitGround()
    {
        ExplodeClientRpc(transform.position);
        Explode();
    }

    public override void OnHitPlayer()
    {
        ExplodeClientRpc(transform.position);
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
            float DistanceFactor = Mathf.Clamp(1 - (Hits[i].transform.position - transform.position).magnitude / Radius, 0.25f, 0.5f);

            Hits[i].GetComponent<PlayerManager>().Damage(OwningPlayerTeam, Damage * DistanceFactor);
            Hits[i].GetComponent<PlayerMovement>().AddVelocity((Hits[i].transform.position + Offset - transform.position).normalized * Impulse * DistanceFactor, true);
        }
    }

    private void ExplodeVisuals()
    {
        Explosion.Play();
    }

    [ClientRpc]
    private void ExplodeClientRpc(Vector3 pos)
    {
        if (IsServer)
        {
            return;
        }

        transform.position = pos;
        ExplodeVisuals();
        Model.SetActive(false);
    }
}
