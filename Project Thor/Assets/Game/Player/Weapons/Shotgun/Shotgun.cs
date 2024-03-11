using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shotgun : BaseWeapon
{
    [Header("// Shotgun //")]

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

        if (!Manager.GetHasAuthority())
        {
            return;
        }

        Manager.ReplicateFire(1);

        if (!Manager.GetIsOwner())
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
                if (hit.transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager stats))
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
                    Manager.GetAimPointLocation() + PlayerMovementComponent.GetRotation() * (Offsets[i - 1] * RandomOffset),
                    PlayerMovementComponent.GetRotation() * (Vector3.forward + Offsets[i - 1] * RandomOffset));

                //PelletRays[i] = new Ray(Manager.GetAimPointLocation(), PlayerMovementComponent.GetRotation() * (Vector3.forward + Vector3.up * Random.Range(-RandomOffset, RandomOffset) + Vector3.right * Random.Range(-RandomOffset, RandomOffset)));
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

                Bullet.SetActive(true);
                tracer.SetPosition(0, PelletRays[i].origin);
                tracer.SetPosition(1, HitPos);
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

        ChargingRocketParticleSystem.Clear();
        ChargingRocketParticleSystem.Stop();

        if (!Manager.GetHasAuthority())
        {
            return;
        }

        GameObject Rocket = Manager.RocketPool.GetPooledObject();

        if (Rocket != null)
        {
            Vector3 Dir = PlayerMovementComponent.GetRotation() * Vector3.forward;
            Rocket.GetComponent<RocketScript>().Init(Manager.GetTeam(), Manager.GetAimPointLocation() + Dir * Offset, Dir
                * Mathf.Clamp(RocketSpeedFactor * (Manager.GetTimeStamp() - ChargingStartTime) / MaxChargingTime, 1, RocketSpeedFactor));
            Rocket.SetActive(true);
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
