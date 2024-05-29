using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorScript : BaseProjectile
{
    public float Radius;
    public AudioSource Sound;

    private Collider[] Hits = new Collider[5];

    public override void Trigger()
    {
        Sound.Play();

        if(!IsServer)
        {
            return;
        }

        int NumHits = Physics.OverlapSphereNonAlloc(SelfTransform.position, Radius, Hits, PlayerLayer);

        for (int i = 0; i < NumHits; i++)
        {
            Hits[i].GetComponent<BasePlayerManager>().Damage(OwningPlayerTeam, Damage);
        }
    }
}
