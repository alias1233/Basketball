using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Unity.Netcode;
using UnityEngine;

public class Pistol : BaseWeapon
{
    [Header("Pistol")]

    public LayerMask ObjectProjectileLayer;

    private Ray BulletRay;

    public AudioSource CoinFlipSound;
    public AudioSource CoinShootSound;

    public override void Start()
    {
        base.Start();
    }

    public override void Fire1()
    {
        Visuals1();

        if (!bIsServer)
        {
            return;
        }

        Manager.ReplicateFire(1);

        Ray CenterRay = new Ray(MuzzlePoint.position, PlayerMovementComponent.GetRotation() * Vector3.forward);

        if (!bIsOwner)
        {
            if (!RewindPlayers(CenterRay, Range1))
            {
                return;
            }
        }

        bool bHit = false;
        RaycastHit hit;

        if (Physics.Raycast(CenterRay, out hit, Range1, PlayerLayer))
        {
            if (hit.transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager stats))
            {
                if (stats.Damage(Manager.GetTeam(), Damage))
                {
                    bHit = true;
                }
            }
        }

        if (bHit)
        {
            Manager.PlayHitSound();
        }

        ResetRewindedPlayers();
    }

    public override void Visuals1()
    {
        if(bIsOwner)
        {
            Manager.Recoil(RecoilRotationAmount1, RecoilPositionAmount1);
        }

        BulletRay = new Ray(Manager.GetAimPointLocation(), PlayerMovementComponent.GetRotation() * Vector3.forward);
        Vector3 HitPos = BulletRay.GetPoint(Range1);
        RaycastHit hit;

        if (Physics.Raycast(BulletRay, out hit, Range1, ObjectProjectileLayer))
        {
            HitPos = hit.point;

            if(hit.transform.TryGetComponent<Coin>(out Coin coin))
            {
                CoinShootSound.Play();

                if (bIsOwner && bIsServer)
                {
                    coin.OnShoot();
                }

                else if(bIsOwner)
                {
                    coin.OnShootServerRpc();
                }
            }
        }

        GameObject Bullet = Manager.BulletPool.GetPooledObject();

        if (Bullet != null)
        {
            LineRenderer tracer = Bullet.GetComponent<LineRenderer>();
            tracer.SetPosition(0, MuzzlePoint.position);
            tracer.SetPosition(1, HitPos);

            Bullet.SetActive(true);
        }
    }

    public override void Fire2()
    {
        Visuals2();

        if (!bIsServer)
        {
            return;
        }

        Manager.ReplicateFire(2);

        GameObject obj = Manager.CoinPool.GetPooledObject();

        if (obj != null)
        {
            Coin coin = obj.GetComponent<Coin>();

            Vector3 vel = PlayerMovementComponent.GetVelocity();

            coin.CoinInit(Manager.GetTeam(), Manager.GetAimPointLocation(),
                PlayerMovementComponent.GetRotation() * Vector3.forward * 0.5f + Vector3.up * 0.5f,
                new Vector3(vel.x, 0, vel.z));
            coin.Spawn();
        }
    }

    public override void Visuals2()
    {
        CoinFlipSound.Play();
    }
}
