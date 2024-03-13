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
    }

    public override void Visuals1()
    {
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
                    Manager.OnShootCoinServerRpc();
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

        GameObject Coin = Manager.ProjectilePool.GetPooledObject();

        if (Coin != null)
        {
            Coin.GetComponent<Coin>().Init(Manager.GetTeam(), Manager.GetAimPointLocation(),
                PlayerMovementComponent.GetRotation() * Vector3.forward + Vector3.up * 0.5f);
            Coin.SetActive(true);
        }
    }

    public override void Visuals2()
    {
        CoinFlipSound.Play();
    }
}
