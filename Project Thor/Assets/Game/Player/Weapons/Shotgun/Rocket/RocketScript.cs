using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RocketScript : BaseProjectile
{
    [Header("Components")]

    [SerializeField]
    private ParticleSystem Explosion;
    [SerializeField]
    private AudioSource ExplosionSound;

    public float Radius;

    public float Impulse;

    public Vector3 Offset;

    Collider[] Hits = new Collider[5];

    public override void OnHitGround()
    {
        ExplodeClientRpc(SelfTransform.position);
        Explode();
    }

    public override void OnHitPlayer()
    {
        ExplodeClientRpc(SelfTransform.position);
        Explode();
    }

    private void Explode()
    {
        ExplodeVisuals();
        Model.SetActive(false);
        Despawn();

        int NumHits = Physics.OverlapSphereNonAlloc(SelfTransform.position, Radius, Hits, PlayerLayer);

        for(int i = 0; i < NumHits; i++)
        {
            float DistanceFactor = Mathf.Clamp(1 - (Hits[i].transform.position - SelfTransform.position).magnitude / Radius, 0.25f, 0.5f);

            Hits[i].GetComponent<PlayerManager>().Damage(OwningPlayerTeam, Damage * DistanceFactor);
            Hits[i].GetComponent<PlayerMovement>().AddVelocity((Hits[i].transform.position + Offset - SelfTransform.position).normalized * Impulse * DistanceFactor, true);
        }
    }

    private void ExplodeVisuals()
    {
        Explosion.Play();
        ExplosionSound.Play();
    }

    [ClientRpc]
    private void ExplodeClientRpc(Vector3 pos)
    {
        if (IsServer)
        {
            return;
        }

        SelfTransform.position = pos;
        ExplodeVisuals();
        Despawn();
    }
}
