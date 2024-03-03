using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketLauncher : BaseWeapon
{
    //[Header("// RocketLauncher //")]

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
            Rocket.GetComponent<RocketScript>().Init(Manager.GetTeam(), MuzzlePoint.position, PlayerMovementComponent.GetRotation() * Vector3.forward);
            Rocket.SetActive(true);
        }
    }

    public override void Visuals1()
    {

    }
}
