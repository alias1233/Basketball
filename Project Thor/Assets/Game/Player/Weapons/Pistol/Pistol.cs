using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pistol : BaseWeapon
{
    [SerializeField]
    private GameObject PistolObject;

    private RaycastHit[] Hits = new RaycastHit[1];

    public override void Start()
    {
        base.Start();
    }

    public override void Fire1()
    {

    }

    public override void Fire2()
    {
        
    }
}
