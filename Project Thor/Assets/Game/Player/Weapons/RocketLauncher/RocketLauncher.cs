using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketLauncher : BaseWeapon
{
    [Header("// RocketLauncher //")]

    public float Offset;

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

        GameObject Rocket = Manager.RocketPool.GetPooledObject();

        if(Rocket != null)
        {
            Quaternion Dir = PlayerMovementComponent.GetRotation();
            Rocket.GetComponent<RocketScript>().Init(Manager.GetTeam(), Manager.GetAimPointLocation() + Dir * Vector3.forward * Offset, Dir);
            Rocket.SetActive(true);
        }
    }

    public override void Visuals1()
    {

    }
}
