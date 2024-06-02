using UnityEngine;

public class TornadoTick : BaseProjectileTick
{
    public float KnockbackFactor;
    public float KnockbackUpFactor;
    public float Damage;

    private float CollidingRadius;
    private Vector3 ColliderOffset1, ColliderOffset2;

    private Collider[] Penetrations = new Collider[5];

    private void Start()
    {
        CapsuleCollider Collider = GetComponent<CapsuleCollider>();
        ColliderOffset1 = Collider.center + Collider.height * 0.5f * Vector3.up + Vector3.down * Collider.radius;
        ColliderOffset2 = Collider.center + Collider.height * 0.5f * Vector3.down + Vector3.up * Collider.radius;
        CollidingRadius = Collider.radius;
    }

    public override void FixedUpdate()
    {
        TimeStamp++;

        if (bIsServer)
        {
            Vector3 Pos = SelfTransform.position;

            int Num = Physics.OverlapCapsuleNonAlloc(Pos + ColliderOffset1, Pos + ColliderOffset2, CollidingRadius, Penetrations, PlayerLayer);

            for(int i = 0; i < Num; i++)
            {
                if(Penetrations[i].GetComponent<BasePlayerManager>().DamageWithKnockback(OwningPlayerTeam, Damage, (Velocity + Vector3.up * KnockbackUpFactor) * KnockbackFactor, false, true))
                {
                    Projectile.OwningPlayer.PlayHitSoundOnOwner();
                }
            }

            SelfTransform.position += Velocity * DeltaTime;

            if (Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
            {
                LastTimeReplicatedPosition = Time.time;

                Projectile.ReplicatePositionClientRpc(SelfTransform.position);
            }

            if (TimeStamp - StartTime >= Lifetime)
            {
                Projectile.ReplicateDisableClientRpc();

                Projectile.Despawn();
            }

            return;
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

        SelfTransform.position += Velocity * DeltaTime;
    }
}
