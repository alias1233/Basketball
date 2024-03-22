using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseProjectileTick : MonoBehaviour
{
    [Header("Cached Components")]

    [HideInInspector]
    public Transform SelfTransform;

    public BaseProjectile Projectile;

    [HideInInspector]
    public bool bIsServer;

    [Header("Movement")]

    public LayerMask PlayerObjectLayer;

    public float InitialSpeed;
    public bool bGravity;
    public float Gravity;
    public bool bBounce;
    public int Bounces;

    [HideInInspector]
    public Vector3 Velocity;

    private float DeltaTime;

    [Header("Stats")]

    [HideInInspector]
    public int TimeStamp;

    public bool DamagesPlayer;

    private float ColliderRadius;

    [HideInInspector]
    public Teams OwningPlayerTeam;

    public int Lifetime;

    [HideInInspector]
    public int StartTime;

    public float ReplicatePositionInterval;

    [HideInInspector]
    public float LastTimeReplicatedPosition;
    [HideInInspector]
    public bool bUpdatedThisFrame;

    private void Awake()
    {
        SelfTransform = transform;
        ColliderRadius = GetComponent<SphereCollider>().radius;
        DeltaTime = Time.fixedDeltaTime;
    }

    // Update is called once per frame
    public virtual void FixedUpdate()
    {
        TimeStamp++;

        if (bIsServer)
        {
            Velocity += Vector3.down * Gravity;

            RaycastHit Hit;

            if (Physics.SphereCast(SelfTransform.position, ColliderRadius, Velocity.normalized, out Hit, (Velocity * DeltaTime).magnitude, PlayerObjectLayer))
            {
                if (Hit.transform.gameObject.layer == 0)
                {
                    SelfTransform.position = Hit.point;
                    Projectile.OnHitGround();

                    return;
                }

                if (Hit.transform.gameObject.layer == 3)
                {
                    if (Hit.transform.TryGetComponent<PlayerManager>(out PlayerManager player))
                    {
                        if (DamagesPlayer)
                        {
                            Projectile.OnHitPlayerWithTarget(player);

                            return;
                        }

                        if (OwningPlayerTeam != player.GetTeam())
                        {
                            SelfTransform.position = Hit.point;
                            Projectile.OnHitPlayer();

                            return;
                        }
                    }
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

        Velocity += Vector3.down * Gravity;
        SelfTransform.position += Velocity * DeltaTime;
    }
}
