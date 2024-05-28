using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeteorTick : BaseProjectileTick
{
    public MeteorScript meteor;
    public int TimeBeforeTrigger;

    public override void FixedUpdate()
    {
        TimeStamp++;

        if (bIsServer)
        {
            if(TimeStamp - StartTime == TimeBeforeTrigger)
            {
                meteor.Trigger();
            }

            if (TimeStamp - StartTime >= Lifetime)
            {
                Projectile.ReplicateDisableClientRpc();
                Projectile.Despawn();
            }

            return;
        }

        if (TimeStamp - StartTime == TimeBeforeTrigger)
        {
            meteor.Trigger();
        }

        if (TimeStamp - StartTime >= Lifetime)
        {
            Projectile.Despawn();
        }

        if (bUpdatedThisFrame)
        {
            bUpdatedThisFrame = false;

            return;
        }
    }
}
