using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class LaserBlaster : BaseWeapon
{
    [SerializeField]
    private GameObject LaserObject;
    [SerializeField]
    private LineRenderer Laser;
    [SerializeField]
    private LineRenderer Laser2;
    [SerializeField]
    private ParticleSystem HitPointParticleSystem;

    [SerializeField]
    private LayerMask ObjectLayer;

    public int Radius2;

    private bool bIsCharging;
    private int ChargingStartTime;

    private float TimeShotLaser2;

    public void Start()
    {
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

        RaycastHit[] Hits2 = new RaycastHit[5];

        int NumHits2 = Physics.RaycastNonAlloc(laserRay, Hits2, Range1, PlayerLayer);

        for (int i = 0; i < NumHits2; i++)
        {
            if (Hits2[i].transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager stats))
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
    }

    public override void StopFire2()
    {
        if(!bIsCharging)
        {
            if(Time.time - TimeShotLaser2 >= 2)
            {
                Laser2.enabled = false;
            }

            return;
        }

        bIsCharging = false;

        Laser2.enabled = true;
        TimeShotLaser2 = Time.time;

        Laser2.SetPosition(0, LaserObject.transform.position);

        Ray laserRay2 = new Ray(LaserObject.transform.position, LaserObject.transform.rotation * (Vector3.forward + Offset));
        RaycastHit colliderInfo2;

        if (Physics.Raycast(laserRay2, out colliderInfo2, Range2, ObjectLayer))
        {
            Laser2.SetPosition(1, colliderInfo2.point);

            return;
        }

        Laser2.SetPosition(1, laserRay2.GetPoint(Range2));

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

        RaycastHit[] Hits2 = new RaycastHit[5];

        int NumHits2 = Physics.SphereCastNonAlloc(laserRay, Radius2, Hits2, Range1, PlayerLayer);

        for (int i = 0; i < NumHits2; i++)
        {
            if (Hits2[i].transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager stats))
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
