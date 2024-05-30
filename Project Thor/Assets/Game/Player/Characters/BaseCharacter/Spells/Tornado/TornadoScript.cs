using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TornadoScript : BaseProjectile
{
    public float KnockbackFactor;
    public float KnockbackUpFactor;
    public AudioSource TornadoSound;

    public override void Activate()
    {
        base.Activate();

        TornadoSound.Play();
    }

    public override void OnHitPlayerWithTarget(BasePlayerManager player)
    {
        player.DamageWithKnockback(OwningPlayerTeam, Damage, (Tick.Velocity + Vector3.up * KnockbackUpFactor) * KnockbackFactor, true);
    }
}
