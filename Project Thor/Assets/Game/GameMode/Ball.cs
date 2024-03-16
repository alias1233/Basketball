using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    public static Ball Singleton { get; internal set; }

    Transform SelfTransform;

    public PlayerMovement AttachedPlayer;
    public bool bAttached;

    public float GravityAcceleration;
    public float FrictionFactor;
    private Vector3 Velocity;

    private float DeltaTime;
    private float ColliderRadius;

    public float SkinWidth = 0.015f;
    public LayerMask layerMask;
    public int MaxBounces = 5;
    public float BounceFactor;
    public int MaxResolvePenetrationAttempts = 3;
    public float ResolvePenetrationDistance = 0.1f;

    private Collider[] Penetrations = new Collider[1];

    private bool bUpdatedThisFrame;

    public float ReplicatePositionInterval;
    private float LastTimeReplicatedPosition;

    public float IgnoreReplicatedMovementTime;
    private float LastTimePredictedMovement;

    private void Awake()
    {
        Singleton = this;

        SelfTransform = transform;
        ColliderRadius = GetComponent<SphereCollider>().radius;

        DeltaTime = Time.fixedDeltaTime;
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            if (bAttached)
            {
                SelfTransform.position = AttachedPlayer.GetHandPosition();

                if (Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
                {
                    LastTimeReplicatedPosition = Time.time;

                    ReplicateAttachClientRpc(AttachedPlayer.gameObject);
                }

                return;
            }

            Velocity = (Velocity + GravityAcceleration * Vector3.down * DeltaTime) * FrictionFactor;

            SelfTransform.position += CollideAndBounce(SelfTransform.position, Velocity * DeltaTime, 0);

            if (Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
            {
                LastTimeReplicatedPosition = Time.time;

                ReplicatePositionClientRpc(SelfTransform.position, Velocity);
            }

            return;
        }

        if(bAttached)
        {
            SelfTransform.position = AttachedPlayer.GetHandPosition();

            return;
        }

        Velocity = (Velocity + GravityAcceleration * Vector3.down * DeltaTime) * FrictionFactor;

        SelfTransform.position += CollideAndBounce(SelfTransform.position, Velocity * DeltaTime, 0);
    }

    public void Detach()
    {
        bAttached = false;
    }

    public void Attach(PlayerMovement playermovement)
    {
        bAttached = true;
        AttachedPlayer = playermovement;

        if (IsClient)
        {
            LastTimePredictedMovement = Time.time;
        }
    }

    public void Throw(Vector3 ThrowVel)
    {
        SelfTransform.position -= ThrowVel.normalized;

        Velocity = ThrowVel;
        bAttached = false;

        int PingInTick = (int)(NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(0) * 0.05f) + 4;

        for (int i = 0; i < PingInTick; i++)
        {
            FixedUpdate();
        }

        if (IsClient)
        {
            LastTimePredictedMovement = Time.time;
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void ReplicatePositionClientRpc(Vector3 position, Vector3 velocity)
    {
        if (IsServer)
        {
            return;
        }

        if (Time.time - LastTimePredictedMovement <= IgnoreReplicatedMovementTime)
        {
            return;
        }

        SelfTransform.position = position;
        Velocity = velocity;
        bAttached = false;

        int PingInTick = (int)(NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(0) * 0.05f) + 4;

        for (int i = 0; i < PingInTick; i++)
        {
            FixedUpdate();
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void ReplicateAttachClientRpc(NetworkObjectReference playerGameObject)
    {
        if (IsServer)
        {
            return;
        }

        if (Time.time - LastTimePredictedMovement <= IgnoreReplicatedMovementTime)
        {
            return;
        }

        if (playerGameObject.TryGet(out NetworkObject networkObject))
        {
            bAttached = true;
            AttachedPlayer = networkObject.GetComponent<PlayerMovement>();
        }
    }

    private Vector3 CollideAndBounce(Vector3 Pos, Vector3 Vel, int depth)
    {
        if(depth >= MaxBounces)
        {
            return Vector3.zero;
        }

        if (Physics.SphereCast(
            Pos,
            ColliderRadius - SkinWidth,
            Vel,
            out RaycastHit hit,
            Vel.magnitude + SkinWidth,
            layerMask
            ))
        {

            Vector3 SnapToSurface = Vel.normalized * (hit.distance + SkinWidth / Mathf.Cos(Vector3.Angle(Vel, hit.normal) * Mathf.PI / 180));

            Velocity = Vector3.Reflect(Vel, hit.normal).normalized * Velocity.magnitude * BounceFactor;

            return SnapToSurface + CollideAndBounce(Pos + SnapToSurface, Vector3.Reflect(Vel - SnapToSurface, hit.normal), depth + 1);
        }

        int PenetrationAttempts = 1;

        while (Physics.OverlapSphereNonAlloc(
            Pos,
            ColliderRadius,
            Penetrations,
            layerMask
            )
            == 1 && PenetrationAttempts <= MaxResolvePenetrationAttempts + 1)
        {
            Vector3 ResolvePenetration = (Pos - Penetrations[0].ClosestPoint(Pos)).normalized * ResolvePenetrationDistance * PenetrationAttempts * PenetrationAttempts;

            if (ResolvePenetration == Vector3.zero)
            {
                ResolvePenetration = (Pos - Penetrations[0].bounds.center).normalized * ResolvePenetrationDistance * PenetrationAttempts * PenetrationAttempts;
            }

            Vel += ResolvePenetration;
            Pos += ResolvePenetration;

            PenetrationAttempts++;
        }

        return Vel;
    }

    public ulong GetAttachedPlayer()
    {
        if(bAttached)
        {
            return AttachedPlayer.GetOwnerID();
        }

        return 10000000000;
    }
}
