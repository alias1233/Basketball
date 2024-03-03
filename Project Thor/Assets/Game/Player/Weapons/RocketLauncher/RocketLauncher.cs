using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketLauncher : BaseWeapon
{
    [Header("// RocketLauncher //")]

    GameObject Rocket;

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
}
