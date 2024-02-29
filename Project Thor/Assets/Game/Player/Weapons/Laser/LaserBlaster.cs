using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LaserBlaster : BaseWeapon
{
    [Header("// LaserBlaser //")]

    [SerializeField]
    private BigLaserScript BigLaser;
    [SerializeField]
    private ParticleSystem ChargingLaserParticleSystem;

    [Header("Fire 1")]

    public int PelletCount;
    public float RandomOffset;

    private Ray[] PelletRays;

    [Header("Fire 2")]

    public int Radius2;
    public int MaxChargingTime;
    private bool bIsCharging;
    private float ChargingStartTime;

    private RaycastHit[] Hits = new RaycastHit[5];

    public override void Start()
    {
        base.Start();

        PelletRays = new Ray[PelletCount];
    }

    public override void Fire1()
    {
        Visuals1();

        if (!Manager.GetHasAuthority())
        {
            return;
        }

        Manager.ReplicateFire(1);

        Ray CenterRay = new Ray(MuzzlePoint.position, PlayerMovementComponent.GetRotation() * (Vector3.forward + Offset));

        if (!Manager.GetIsOwner())
        {
            if (!RewindPlayers(CenterRay, Range1))
            {
                return;
            }
        }

        for (int i = 0; i < PelletCount; i++)
        {
            RaycastHit hit;

            if (Physics.Raycast(PelletRays[i], out hit, Range1, PlayerLayer))
            {
                if (hit.transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager stats))
                {
                    stats.Damage(Manager.GetTeam(), Damage);
                }
            }
        }

        ResetRewindedPlayers();
    }

    public override void Visuals1()
    {
        for (int i = 0; i < PelletCount; i++)
        {
            if(i == 0)
            {
                PelletRays[i] = new Ray(MuzzlePoint.position, PlayerMovementComponent.GetRotation() * (Vector3.forward + Offset));
            }

            else
            {
                PelletRays[i] = new Ray(MuzzlePoint.position, PlayerMovementComponent.GetRotation()
                * (Vector3.forward + Offset + Vector3.up * Random.Range(-RandomOffset, RandomOffset) + Vector3.right * Random.Range(-RandomOffset, RandomOffset)));
            }

            Vector3 HitPos = PelletRays[i].GetPoint(Range1);
            RaycastHit hit;

            if (Physics.Raycast(PelletRays[i], out hit, Range1, ObjectLayer))
            {
                HitPos = hit.point;
            }

            GameObject Bullet = Manager.BulletPool.GetPooledObject();

            if (Bullet != null)
            {
                if (Bullet.TryGetComponent<LineRenderer>(out LineRenderer tracer))
                {
                    Bullet.SetActive(true);

                    tracer.SetPosition(0, MuzzlePoint.position);
                    tracer.SetPosition(1, HitPos);
                }
            }
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

        LastTimeShot2 = Manager.GetTimeStamp();

        bIsCharging = false;

        ChargingLaserParticleSystem.Clear();
        ChargingLaserParticleSystem.Stop();

        Visuals2();

        if (!Manager.GetHasAuthority())
        {
            return;
        }

        Manager.ReplicateFire(2);

        Ray laserRay = new Ray(MuzzlePoint.position, PlayerMovementComponent.GetRotation() * (Vector3.forward + Offset));

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
                stats.Damage(Manager.GetTeam(), Mathf.Clamp(Damage2 * (Manager.GetTimeStamp() - ChargingStartTime) / MaxChargingTime, 0, Damage2));
            }
        }

        ResetRewindedPlayers();
    }

    public override void Visuals2()
    {
        Ray laserRay2 = new Ray(MuzzlePoint.position, PlayerMovementComponent.GetRotation() * (Vector3.forward + Offset));
        RaycastHit colliderInfo2;

        if (Physics.Raycast(laserRay2, out colliderInfo2, Range2, ObjectLayer))
        {
            BigLaser.ShootLaser(colliderInfo2.distance / 2, MuzzlePoint.rotation * (Vector3.forward + Offset));
        }

        else
        {
            BigLaser.ShootLaser(Range2 / 2, MuzzlePoint.rotation * (Vector3.forward + Offset));
        }
    }

    public override void OnActivate()
    {
        bIsCharging = false;
    }
    public override void OnDeactivate()
    {
        ChargingLaserParticleSystem.Clear();
        ChargingLaserParticleSystem.Stop();
    }
}
