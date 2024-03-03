using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sword : BaseWeapon
{
    [Header("// LaserBlaser //")]

    //[SerializeField]
    //private SwordAnimScript swordanimation;

    [Header("Fire 1")]

    public float Radius;

    private RaycastHit[] Hits = new RaycastHit[5];

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

        Manager.ReplicateFire(1);

        Ray CenterRay = new Ray(Manager.GetAimPointLocation(), PlayerMovementComponent.GetRotation() * Vector3.forward);

        if (!Manager.GetIsOwner())
        {
            if (!RewindPlayers(CenterRay, Range1))
            {
                return;
            }
        }

        int NumHits = Physics.SphereCastNonAlloc(CenterRay, Radius, Hits, Range1, PlayerLayer);

        for (int i = 0; i < NumHits; i++)
        {
            if (Hits[i].transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager stats))
            {
                stats.Damage(Manager.GetTeam(), Damage);
            }
        }

        ResetRewindedPlayers();
    }

    public override void Visuals1()
    {
        //swordanimation.SwingAnim();
    }

    public override void Fire2()
    {

    }

    public override void StopFire2()
    {

    }

    public override void Visuals2()
    {

    }
}
