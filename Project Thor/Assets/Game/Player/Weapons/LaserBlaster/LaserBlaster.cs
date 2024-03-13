using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserBlaster : BaseWeapon
{
    [Header("// LaserBlaster //")]

    [SerializeField]
    private BigLaserScript BigLaser;
    [SerializeField]
    private ParticleSystem ChargingLaserParticleSystem;

    public int Radius2;
    public int MaxChargingTime;
    private bool bIsCharging;
    private float ChargingStartTime;
    private RaycastHit[] Hits = new RaycastHit[5];

    public override void Start()
    {
        base.Start();
    }

    public override void Fire1()
    {
        Visuals1();

        if (!Manager.GetHasAuthority())
        {
            return;
        }
    }

    public override void Visuals1()
    {

    }

    public override void Fire2()
    {
        if (bIsCharging)
        {
            return;
        }

        bIsCharging = true;
        ChargingStartTime = Manager.GetTimeStamp();

        ChargingLaserParticleSystem.Play();
    }

    public override void StopFire2()
    {
        if (!bIsCharging)
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

        Ray laserRay = new Ray(MuzzlePoint.position, PlayerMovementComponent.GetRotation() * Vector3.forward);

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
        Ray laserRay2 = new Ray(MuzzlePoint.position, PlayerMovementComponent.GetRotation() * Vector3.forward);
        RaycastHit colliderInfo2;

        if (Physics.Raycast(laserRay2, out colliderInfo2, Range2, ObjectLayer))
        {
            BigLaser.ShootLaser(colliderInfo2.distance / 2, MuzzlePoint.rotation * Vector3.forward);
        }

        else
        {
            BigLaser.ShootLaser(Range2 / 2, MuzzlePoint.rotation * Vector3.forward);
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
