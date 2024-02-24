using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Pistol : BaseWeapon
{
    [SerializeField]
    private GameObject PistolObject;

    private RaycastHit[] Hits = new RaycastHit[1];

    public void Start()
    {

    }

    public override void Fire1()
    {
        Ray HitscanRay = new Ray(PistolObject.transform.position, PlayerMovementComponent.GetRotation() * (Vector3.forward + Offset));

        int NumHits2 = Physics.RaycastNonAlloc(HitscanRay, Hits, Range1, PlayerLayer);

        Debug.DrawRay(PistolObject.transform.position, PlayerMovementComponent.GetRotation() * (Vector3.forward + Offset), Color.red, 10);

        if(NumHits2 > 0)
        {
            Manager.HitPlayerServerRPC(Hits[0].transform.gameObject);
        }
    }

    public override void Fire2()
    {
        
    }
}
