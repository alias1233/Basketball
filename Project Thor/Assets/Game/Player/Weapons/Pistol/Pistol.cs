using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pistol : BaseWeapon
{
    [SerializeField]
    private GameObject PistolObject;

    public void Start()
    {

    }

    public override void Fire1()
    {
        Ray laserRay = new Ray(PistolObject.transform.position, PlayerMovementComponent.GetRotation() * (Vector3.forward + Offset));

        RaycastHit[] Hits2 = new RaycastHit[1];

        int NumHits2 = Physics.RaycastNonAlloc(laserRay, Hits2, Range1, PlayerLayer);

        if(NumHits2 > 0)
        {
            Manager.HitPlayerServerRPC(Hits2[0].transform.gameObject);
        }
    }

    public override void Fire2()
    {
        
    }
}
