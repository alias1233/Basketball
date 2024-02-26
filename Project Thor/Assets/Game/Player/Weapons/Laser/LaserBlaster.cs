using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class LaserBlaster : BaseWeapon
{
    [SerializeField]
    private GameObject LaserObject;
    [SerializeField]
    private LineRenderer Laser;
    [SerializeField]
    private ParticleSystem HitPointParticleSystem;
    [SerializeField]
    private BigLaserScript BigLaser;
    [SerializeField]
    private ParticleSystem ChargingLaserParticleSystem;

    [SerializeField]
    private LayerMask ObjectLayer;

    public int Radius2;

    private bool bIsCharging;
    private int ChargingStartTime;

    private RaycastHit[] Hits = new RaycastHit[5];

    public override void Start()
    {
        base.Start();

        Laser = LaserObject.GetComponent<LineRenderer>();
    }

    public override void Fire1()
    {
        if(!Laser.enabled)
        {
            Laser.enabled = true;
        }

        if (!Manager.GetHasAuthority())
        {
            return;
        }

        Ray laserRay = new Ray(LaserObject.transform.position, PlayerMovementComponent.GetRotation() * (Vector3.forward + Offset));

        if(!Manager.GetIsOwner())
        {
            if (!RewindPlayers(laserRay, Range1))
            {
                return;
            }
        }

        int NumHits = Physics.RaycastNonAlloc(laserRay, Hits, Range1, PlayerLayer);

        for (int i = 0; i < NumHits; i++)
        {
            if (Hits[i].transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager stats))
            {
                stats.Damage(Manager.GetTeam(), Damage);
            }
        }

        ResetRewindedPlayers();
    }

    public override void StopFire1()
    {
        if (Laser.enabled)
        {
            Laser.enabled = false;
            HitPointParticleSystem.Stop();
        }
    }

    public override void Fire2()
    {
        if(bIsCharging)
        {
            return;
        }

        bIsCharging = true;
        ChargingStartTime = Manager.GetTimeStamp();

        ChargingLaserParticleSystem.Play();
    }

    public override void StopFire2()
    {
        if(!bIsCharging)
        {
            return;
        }

        bIsCharging = false;

        LastTimeShot2 = Manager.GetTimeStamp();

        ChargingLaserParticleSystem.Clear();
        ChargingLaserParticleSystem.Stop();

        Ray laserRay2 = new Ray(LaserObject.transform.position, LaserObject.transform.rotation * (Vector3.forward + Offset));
        RaycastHit colliderInfo2;

        if (Physics.Raycast(laserRay2, out colliderInfo2, Range2, ObjectLayer))
        {
            BigLaser.ShootLaser(colliderInfo2.distance / 2);
        }

        else
        {
            BigLaser.ShootLaser(Range2 / 2);
        }

        if (!Manager.GetHasAuthority())
        {
            return;
        }

        Ray laserRay = new Ray(LaserObject.transform.position, PlayerMovementComponent.GetRotation() * (Vector3.forward + Offset));

        if (!Manager.GetIsOwner())
        {
            if (!RewindPlayers(laserRay, Range2))
            {
                return;
            }
        }

        int NumHits = Physics.SphereCastNonAlloc(laserRay, Radius2, Hits, Range1, PlayerLayer);

        for (int i = 0; i < NumHits; i++)
        {
            if (Hits[i].transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager stats))
            {
                stats.Damage(Manager.GetTeam(), Damage2);
            }
        }

        ResetRewindedPlayers();
    }

    public override void OnActivate()
    {
        bIsCharging = false;
    }
    public override void OnDeactivate()
    {
        Laser.enabled = false;
        HitPointParticleSystem.Stop();
        ChargingLaserParticleSystem.Clear();
        ChargingLaserParticleSystem.Stop();
    }

    public void LateUpdate()
    {
        if (!Laser.enabled)
        {
            return;
        }

        Laser.SetPosition(0, LaserObject.transform.position);

        Ray laserRay = new Ray(LaserObject.transform.position, LaserObject.transform.rotation * (Vector3.forward + Offset));
        RaycastHit colliderInfo;

        if (Physics.Raycast(laserRay, out colliderInfo, Range1, ObjectLayer))
        {
            Laser.SetPosition(1, colliderInfo.point);

            if (!HitPointParticleSystem.isPlaying)
            {
                HitPointParticleSystem.Play();
            }

            HitPointParticleSystem.transform.position = colliderInfo.point;

            return;
        }

        Vector3 endpointpos = laserRay.GetPoint(Range1);

        Laser.SetPosition(1, endpointpos);

        if (HitPointParticleSystem.isPlaying)
        {
            HitPointParticleSystem.Stop();
        }
    }
}
