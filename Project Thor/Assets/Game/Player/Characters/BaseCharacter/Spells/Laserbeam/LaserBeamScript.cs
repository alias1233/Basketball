using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBeamScript : BaseProjectile
{
    public BigLaserScript BigLaser;
    public AudioSource LaserSound;
    public AudioSource ElectricSound;
    public float LaserRange;
    public float LaserRadius;

    public RaycastHit[] Hits = new RaycastHit[5];

    public GameObject InitialElectricity;

    public override void Activate()
    {
        base.Activate();

        InitialElectricity.SetActive(true);
        ElectricSound.Play();
    }

    public override void Trigger()
    {
        InitialElectricity.SetActive(false);
        LaserSound.Play();

        RaycastHit colliderInfo2;

        if (Physics.Raycast(SelfTransform.position, SelfTransform.forward, out colliderInfo2, LaserRange, ObjectLayer))
        {
            BigLaser.ShootLaser(colliderInfo2.distance / 2, SelfTransform.forward);

            if(IsServer)
            {
                int NumHits = Physics.SphereCastNonAlloc(SelfTransform.position, LaserRadius, SelfTransform.forward, Hits, colliderInfo2.distance, PlayerLayer);

                for (int i = 0; i < NumHits; i++)
                {
                    if (Hits[i].transform.gameObject.TryGetComponent<BasePlayerManager>(out BasePlayerManager stats))
                    {
                        stats.Damage(OwningPlayerTeam, Damage);
                    }
                }
            }
        }
        else
        {
            BigLaser.ShootLaser(LaserRange / 2, SelfTransform.forward);

            if (IsServer)
            {
                int NumHits = Physics.SphereCastNonAlloc(SelfTransform.position, LaserRadius, SelfTransform.forward, Hits, LaserRange, PlayerLayer);

                for (int i = 0; i < NumHits; i++)
                {
                    if (Hits[i].transform.gameObject.TryGetComponent<BasePlayerManager>(out BasePlayerManager stats))
                    {
                        stats.Damage(OwningPlayerTeam, Damage);
                    }
                }
            }
        }
    }
}
