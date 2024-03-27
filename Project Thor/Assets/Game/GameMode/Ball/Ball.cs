using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Ball : NetworkBehaviour
{
    public static Ball Singleton { get; internal set; }

    Transform SelfTransform;

    [HideInInspector]
    public PlayerManager AttachedPlayer;

    [SerializeField]
    Transform ModelTransform;
    [HideInInspector]
    public bool bAttached;

    public Transform SpawnLocationTransform;

    public Transform[] RespawnLocations;

    public float GravityAcceleration;
    [HideInInspector]
    public Vector3 Velocity;

    private float DeltaTime;
    private float ColliderRadius;

    public float SkinWidth = 0.015f;
    public LayerMask layerMask;
    public int MaxBounces = 5;
    public float BounceFactor;
    public int MaxResolvePenetrationAttempts = 3;
    public float ResolvePenetrationDistance = 0.1f;

    private Collider[] Penetrations = new Collider[1];

    public float ReplicatePositionInterval;
    private float LastTimeReplicatedPosition;

    public float IgnoreReplicatedMovementTime;
    private float LastTimePredictedMovement;

    public float NYPos;

    private void Awake()
    {
        Singleton = this;

        SelfTransform = transform;
        ColliderRadius = GetComponent<SphereCollider>().radius - SkinWidth;

        DeltaTime = Time.fixedDeltaTime;
    }

    private void Update()
    {
        if (bAttached)
        {
            SelfTransform.position = AttachedPlayer.GetHandPosition();
        }
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            Vector3 pos = SelfTransform.position;

            if (pos.y < NYPos)
            {
                if (bAttached)
                {
                    Detach();
                }

                float MinDistance = 9999999999999;
                Vector3 RespawnLocation = Vector3.zero;
                Vector3 Pos = SelfTransform.position;

                foreach (Transform i in RespawnLocations)
                {
                    float distance = (i.position - Pos).magnitude;

                    if (distance < MinDistance)
                    {
                        MinDistance = distance;
                        RespawnLocation = i.position;
                    }
                }

                SelfTransform.position = RespawnLocation;
                Velocity = Vector3.zero;
            }

            /*
            if(pos.x < NXPos)
            {
                if(bAttached)
                {
                    Detach();
                }
                SelfTransform.position = new Vector3(NXPos + 2, SelfTransform.position.y, SelfTransform.position.z);
                Velocity = Vector3.zero;
            }
            if (pos.x > XPos)
            {
                if (bAttached)
                {
                    Detach();
                }
                SelfTransform.position = new Vector3(XPos - 2, SelfTransform.position.y, SelfTransform.position.z);
                Velocity = Vector3.zero;
            }
            if (pos.y < NYPos)
            {
                if (bAttached)
                {
                    Detach();
                }
                SelfTransform.position = new Vector3(SelfTransform.position.x, NYPos + 2, SelfTransform.position.z);
                Velocity = Vector3.zero;
            }
            if (pos.y > YPos)
            {
                if (bAttached)
                {
                    Detach();
                }
                SelfTransform.position = new Vector3(SelfTransform.position.x, YPos - 2, SelfTransform.position.z);
                Velocity = Vector3.zero;
            }
            if (pos.z < NZPos)
            {
                if (bAttached)
                {
                    Detach();
                }
                SelfTransform.position = new Vector3(SelfTransform.position.x, SelfTransform.position.y, NZPos + 2);
                Velocity = Vector3.zero;
            }
            if (pos.z > ZPos)
            {
                if (bAttached)
                {
                    Detach();
                }
                SelfTransform.position = new Vector3(SelfTransform.position.x, SelfTransform.position.y, ZPos - 2);
                Velocity = Vector3.zero;
            }
            */

            if (bAttached)
            {
                if (Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
                {
                    LastTimeReplicatedPosition = Time.time;

                    ReplicateAttachClientRpc(AttachedPlayer.gameObject);
                }

                return;
            }

            Velocity = Velocity + GravityAcceleration * Vector3.down * DeltaTime;

            SelfTransform.position += CollideAndBounce(SelfTransform.position, Velocity * DeltaTime, 0);

            ModelTransform.Rotate(Vector3.Cross(Vector3.up, Velocity.normalized),
                Mathf.Abs(Velocity.x) + Mathf.Abs(Velocity.z),
                Space.World);

            if (Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
            {
                LastTimeReplicatedPosition = Time.time;

                ReplicatePositionClientRpc(SelfTransform.position, Velocity);
            }

            return;
        }

        if(bAttached)
        {
            return;
        }

        Velocity = Velocity + GravityAcceleration * Vector3.down * DeltaTime;

        SelfTransform.position += CollideAndBounce(SelfTransform.position, Velocity * DeltaTime, 0);

        ModelTransform.Rotate(Vector3.Cross(Vector3.up, Velocity.normalized),
                 Mathf.Abs(Velocity.x) + Mathf.Abs(Velocity.z),
                 Space.World);
    }

    public void Detach()
    {
        bAttached = false;

        SelfTransform.position = AttachedPlayer.GetAimPointLocation();

        AttachedPlayer.Unattach();
    }

    public void Attach(PlayerManager player)
    {
        if(player.GetIsFlying())
        {
            return;
        }

        bAttached = true;

        if(AttachedPlayer != null)
        {
            AttachedPlayer.Unattach();
        }

        AttachedPlayer = player;
        AttachedPlayer.Attach();

        if (IsClient)
        {
            LastTimePredictedMovement = Time.time;
        }
    }

    public void Throw(Vector3 ThrowVel)
    {
        SelfTransform.position = AttachedPlayer.GetAimPointLocation();

        Velocity = ThrowVel;
        bAttached = false;
        AttachedPlayer.Unattach();

        int PingInTick = (int)(NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(0) * 0.05f) + 3;

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

        if (AttachedPlayer != null)
        {
            AttachedPlayer.Unattach();
        }

        int PingInTick = (int)(NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(0) * 0.05f) + 3;

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
            PlayerManager newattachedplayer = networkObject.GetComponent<PlayerManager>();

            if (AttachedPlayer != null)
            {
                if(AttachedPlayer.GetClientID() != newattachedplayer.GetClientID())
                {
                    AttachedPlayer.Unattach();
                }
            }

            bAttached = true;
            AttachedPlayer = newattachedplayer;
            AttachedPlayer.Attach();
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
            ColliderRadius,
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
            return AttachedPlayer.OwnerClientId;
        }

        return 10000000000;
    }

    public void TeleportTo(Vector3 pos)
    {
        SelfTransform.position = pos;
    }
}
