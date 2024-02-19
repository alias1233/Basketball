using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class LaserBlaster : BaseWeapon
{
    [SerializeField]
    private GameObject LaserObject;
    [SerializeField]
    private LineRenderer Laser;

    [SerializeField]
    private LayerMask ObjectLayer;

    public void Start()
    {
        Laser = LaserObject.GetComponent<LineRenderer>();
    }

    public override void Fire1()
    {
        Laser.enabled = true;

        Ray laserRay = new Ray(LaserObject.transform.position, LaserObject.transform.forward);

        if (!Manager.GetHasAuthority())
        {
            return;
        }

        if (!Manager.GetIsOwner())
        {
            RewindedPlayerList.Clear();

            RaycastHit[] Hits = new RaycastHit[5];

            int NumHits = Physics.SphereCastNonAlloc(laserRay, Manager.GetRadius(), Hits, Range, PlayerLayer);

            for (int i = 0; i < NumHits; i++)
            {
                if (Hits[i].transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager rewind))
                {
                    if (rewind.RewindToPosition(Manager.GetTeam(), Manager.GetPingInTick()))
                    {
                        RewindedPlayerList.Add(rewind);
                    }
                }
            }

            Physics.SyncTransforms();
        }

        RaycastHit[] Hits2 = new RaycastHit[5];

        int NumHits2 = Physics.RaycastNonAlloc(laserRay, Hits2, Range, PlayerLayer);

        for (int i = 0; i < NumHits2; i++)
        {
            if (Hits2[i].transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager stats))
            {
                stats.Damage(Manager.GetTeam(), Damage);
            }
        }

        if (!Manager.GetIsOwner())
        {
            foreach(PlayerManager i in RewindedPlayerList)
            {
                i.ResetToOriginalPosition();
            }
        }
    }

    public override void StopFire1()
    {
        Laser.enabled = false;
    }

    public void LateUpdate()
    {
        if (!Laser.enabled)
        {
            return;
        }

        Laser.SetPosition(0, LaserObject.transform.position);

        Ray laserRay = new Ray(LaserObject.transform.position, LaserObject.transform.forward);
        RaycastHit colliderInfo;

        if (Physics.Raycast(laserRay, out colliderInfo, Range, ObjectLayer))
        {
            Laser.SetPosition(1, colliderInfo.point);

            return;
        }

        Laser.SetPosition(1, laserRay.GetPoint(100));
    }
}
