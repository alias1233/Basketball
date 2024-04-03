using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingHookTick : BaseProjectileTick
{
    public GrapplingHook Grapple;

    public override void FixedUpdate()
    {
        if(Grapple.bHit)
        {
            if(Grapple.OwningPlayerMovement)
            {
                Grapple.grapple.SetPosition(0, Grapple.OwningPlayerMovement.GetGrappleShootStartLocation());
                Grapple.grapple.SetPosition(1, SelfTransform.position);
            }

            return;
        }

        base.FixedUpdate();

        if (!Grapple.OwningPlayerMovement)
        {
            return;
        }

        Grapple.grapple.SetPosition(0, Grapple.OwningPlayerMovement.GetGrappleShootStartLocation());
        Grapple.grapple.SetPosition(1, SelfTransform.position);
    }
}
