using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : BaseWeapon
{
    [Header("// Shotgun //")]

    public int Range1;

    public float Damage;

    public Vector3 RecoilRotationAmount1;
    public Vector3 RecoilPositionAmount1;

    public Vector3 RecoilRotationAmount2;
    public Vector3 RecoilPositionAmount2;

    [SerializeField]
    private ParticleSystem ChargingRocketParticleSystem;

    [Header("Fire 1")]

    public int PelletCount;
    public float RandomOffset;

    private Ray[] PelletRays;

    public Vector3[] Offsets;

    [Header("Fire 2")]

    public float Offset;

    public int MaxChargingTime;
    private bool bIsCharging;
    private float ChargingStartTime;

    public float RocketSpeedFactor;

    public override void Start()
    {
        base.Start();

        PelletRays = new Ray[PelletCount];
    }

    public override void Fire1()
    {
        Visuals1();

        if (!bIsServer)
        {
            return;
        }

        Manager.ReplicateFire(1);

        if (!bIsOwner)
        {
            if (!RewindPlayers(new Ray(MuzzlePoint.position, PlayerMovementComponent.GetRotation() * Vector3.forward), Range1))
            {
                return;
            }
        }

        bool bHit = false;

        for (int i = 0; i < PelletCount; i++)
        {
            RaycastHit hit;

            if (Physics.Raycast(PelletRays[i], out hit, Range1, PlayerLayer))
            {
                if (hit.transform.gameObject.TryGetComponent<BasePlayerManager>(out BasePlayerManager stats))
                {
                    if(stats.Damage(Manager.GetTeam(), Damage))
                    {
                        bHit = true;
                    }
                }
            }
        }

        if(bHit)
        {
            Manager.PlayHitSound();
        }

        ResetRewindedPlayers();
    }

    public override void Visuals1()
    {
        ShootSound1.Play();

        for (int i = 0; i < PelletCount; i++)
        {
            if(i == 0)
            {
                PelletRays[i] = new Ray(Manager.GetAimPointLocation(), PlayerMovementComponent.GetRotation() * Vector3.forward);
            }

            else
            {
                PelletRays[i] = new Ray(
                    Manager.GetAimPointLocation(),
                    PlayerMovementComponent.GetRotation() * (Vector3.forward + Offsets[i - 1] * RandomOffset));
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
                LineRenderer tracer = Bullet.GetComponent<LineRenderer>();

                if(i == 0)
                {
                    tracer.SetPosition(0, MuzzlePoint.position);
                }

                else
                {
                    tracer.SetPosition(0, MuzzlePoint.position + PlayerMovementComponent.GetRotation() * (Offsets[i - 1] * RandomOffset));
                }
                
                tracer.SetPosition(1, HitPos);

                Bullet.SetActive(true);
            }
        }

        if(bIsOwner)
        {
            Manager.Recoil(RecoilRotationAmount1, RecoilPositionAmount1);
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

        ChargingRocketParticleSystem.Play();
    }

    public override void StopFire2()
    {
        if(!bIsCharging)
        {
            return;
        }

        LastTimeShot2 = Manager.GetTimeStamp();

        bIsCharging = false;

        if (bIsOwner)
        {
            Manager.Recoil(RecoilRotationAmount2, RecoilPositionAmount2);
        }

        ChargingRocketParticleSystem.Clear();
        ChargingRocketParticleSystem.Stop();

        if (!bIsServer)
        {
            return;
        }

        GameObject obj = Manager.RocketPool.GetPooledObject();

        if (obj != null)
        {
            Vector3 Dir = PlayerMovementComponent.GetRotation() * Vector3.forward;
            RocketScript rocket = obj.GetComponent<RocketScript>();
            rocket.Init(Manager.GetTeam(), Manager.GetAimPointLocation() + Dir * Offset, Dir
                * Mathf.Clamp(RocketSpeedFactor * (Manager.GetTimeStamp() - ChargingStartTime) / MaxChargingTime, 1, RocketSpeedFactor));
            rocket.Spawn();
        }
    }

    public override void OnActivate()
    {
        bIsCharging = false;
    }
    public override void OnDeactivate()
    {
        ChargingRocketParticleSystem.Clear();
        ChargingRocketParticleSystem.Stop();
    }
}
