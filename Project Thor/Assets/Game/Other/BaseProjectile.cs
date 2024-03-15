using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BaseProjectile : NetworkBehaviour, IBaseNetworkObject
{
    public bool bIsActive
    {
        get
        {
            return bisactive;
        }

        set
        {
            bisactive = value;
        }
    }

    public bool bisactive;

    public void Spawn()
    {
        bIsActive = true;

        Activate();
    }

    public void Despawn()
    {
        bIsActive = false;

        Deactivate();
    }

    virtual public void Activate()
    {
        Model.SetActive(true);
    }

    virtual public void Deactivate()
    {
        Model.SetActive(false);
    }

    [Header("Cached Components")]

    [HideInInspector]
    public Transform SelfTransform;

    [Header("Components")]

    public GameObject Model;

    [Header("Movement")]

    public float InitialSpeed;
    public bool bGravity;
    public float Gravity;
    public bool bBounce;
    public int Bounces;

    [HideInInspector]
    public Vector3 Velocity;

    private float DeltaTime;

    [Header("Stats")]

    public bool DamagesPlayer;

    private float ColliderRadius;

    [HideInInspector]
    public Teams OwningPlayerTeam;

    public float Lifetime;

    [HideInInspector]
    public float StartTime;

    public float ReplicatePositionInterval;

    [HideInInspector]
    public float LastTimeReplicatedPosition;
    [HideInInspector]
    public bool bUpdatedThisFrame;

    public float Damage;
    public LayerMask PlayerLayer;
    public LayerMask PlayerObjectLayer;

    public virtual void Awake()
    {
        SelfTransform = transform;
        ColliderRadius = GetComponent<SphereCollider>().radius;
        DeltaTime = Time.fixedDeltaTime;
    }

    // Update is called once per frame
    public virtual void FixedUpdate()
    {
        if (!bIsActive)
        {
            return;
        }

        if (IsServer)
        {
            Velocity += Vector3.down * Gravity;

            RaycastHit Hit;

            if(Physics.SphereCast(SelfTransform.position, ColliderRadius, Velocity.normalized, out Hit, (Velocity * DeltaTime).magnitude, PlayerObjectLayer))
            {
                if (DamagesPlayer)
                {
                    if (Hit.transform.TryGetComponent<PlayerManager>(out PlayerManager player))
                    {
                        SelfTransform.position = Hit.point;
                        OnHitPlayerWithTarget(player);

                        return;
                    }

                    else
                    {
                        SelfTransform.position = Hit.point;
                        OnHitGround();

                        return;
                    }
                }

                else
                {
                    if (Hit.transform.TryGetComponent<PlayerManager>(out PlayerManager player))
                    {
                        if (OwningPlayerTeam != player.GetTeam())
                        {
                            SelfTransform.position = Hit.point;
                            OnHitPlayer();

                            return;
                        }
                    }

                    else
                    {
                        SelfTransform.position = Hit.point;
                        OnHitGround();

                        return;
                    }
                }
            }

            SelfTransform.position += Velocity * DeltaTime;

            if(Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
            {
                LastTimeReplicatedPosition = Time.time;

                ReplicatePositionClientRpc(SelfTransform.position);
            }

            if(Time.time - StartTime >= Lifetime)
            {
                ReplicateDisableClientRpc();

                Despawn();
            }

            return;
        }

        if(bUpdatedThisFrame)
        {
            bUpdatedThisFrame = false;

            return;
        }

        Velocity += Vector3.down * Gravity;
        SelfTransform.position += Velocity * DeltaTime;
    }

    public virtual void OnHitGround() { }

    public virtual void OnHitPlayer() { }

    public virtual void OnHitPlayerWithTarget(PlayerManager player) { }

    public virtual void Init(Teams team, Vector3 pos, Vector3 dir)
    {
        LastTimeReplicatedPosition = Time.time;
        InitClientRpc(team, pos, dir);
        StartTime = LastTimeReplicatedPosition;

        OwningPlayerTeam = team;
        SelfTransform.position = pos;
        Velocity = dir * InitialSpeed;
        SelfTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateDisableClientRpc()
    {
        if (IsServer)
        {
            return;
        }

        Despawn();
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void ReplicatePositionClientRpc(Vector3 pos)
    {
        if(IsServer)
        {
            return;
        }

        if (!bIsActive)
        {
            Spawn();
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void InitClientRpc(Teams team, Vector3 pos, Vector3 dir)
    {
        if (IsServer)
        {
            return;
        }

        if (!bIsActive)
        {
            Spawn();
        }

        bUpdatedThisFrame = true;

        OwningPlayerTeam = team;
        SelfTransform.position = pos;
        Velocity = dir * InitialSpeed;
        SelfTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }
}
