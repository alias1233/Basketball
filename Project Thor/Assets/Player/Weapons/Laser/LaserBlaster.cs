using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class LaserBlaster : BaseWeapon
{
    [SerializeField]
    private GameObject LaserObject;
    [SerializeField]
    private LineRenderer Laser;

    public void Start()
    {
        Laser = LaserObject.GetComponent<LineRenderer>();
    }

    public override void Fire1()
    {
        if (!Laser.enabled)
        {
            Laser.enabled = true;

            if (!Manager.GetHasAuthority())
            {
                Manager.StartFiring1ServerRpc();

                return;
            }
        }

        Ray laserRay = new Ray(LaserObject.transform.position, PlayerMovementComponent.GetRotation() * Vector3.forward);
        RaycastHit colliderInfo;

        if (Physics.Raycast(laserRay, out colliderInfo, 1000))
        {
             
        }
    }

    public override void StopFire1()
    {
        if (Laser.enabled)
        {
            Laser.enabled = false;

            if (Manager.GetHasAuthority())
            {
                return;
            }

            Manager.StopFiring1ServerRpc();
        }
    }

    public void Update()
    {
        if (!Laser.enabled)
        {
            return;
        }

        Laser.SetPosition(0, LaserObject.transform.position);

        Ray laserRay = new Ray(LaserObject.transform.position, LaserObject.transform.forward);
        RaycastHit colliderInfo;

        if (Physics.Raycast(laserRay, out colliderInfo, 1000))
        {
            Laser.SetPosition(1, colliderInfo.point);

            return;
        }

        Laser.SetPosition(1, laserRay.GetPoint(1000));
    }
}
