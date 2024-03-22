using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrapplingHookTick : BaseProjectileTick
{
    new public GrapplingHook Grapple;

    public override void FixedUpdate()
    {
        if (!Projectile.bIsActive)
        {
            if (Grapple.bHit)
            {
                Grapple.grapple.SetPosition(0, Grapple.OwningPlayerMovement.GetPosition());
                Grapple.grapple.SetPosition(1, SelfTransform.position);
            }

            return;
        }

        base.FixedUpdate();

        Grapple.grapple.SetPosition(0, Grapple.OwningPlayerMovement.GetPosition());
        Grapple.grapple.SetPosition(1, SelfTransform.position);
    }
}
