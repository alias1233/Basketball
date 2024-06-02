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
        int NumHits = Physics.OverlapSphereNonAlloc(SelfTransform.position, Radius, Hits, PlayerLayer);

        for(int i = 0; i < NumHits; i++)
        {
            float DistanceFactor = Mathf.Clamp(1 - (Hits[i].transform.position - SelfTransform.position).magnitude / Radius, 0.25f, 0.5f);

            if(Hits[i].GetComponent<BasePlayerManager>().DamageWithKnockback(OwningPlayerTeam, Damage * DistanceFactor,
                (Hits[i].transform.position + Offset - SelfTransform.position).normalized * Impulse * DistanceFactor, true, true))
            {
                OwningPlayer.PlayHitSoundOnOwner();
            }
        }

        ExplodeVisuals();
        Model.SetActive(false);
        Despawn();
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
