using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BaseProjectile : NetworkBehaviour, IBaseNetworkObject
{
    [HideInInspector]
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
    [HideInInspector]
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
        Tick.enabled = true;
    }

    virtual public void Deactivate()
    {
        Model.SetActive(false);
        Tick.enabled = false;
    }

    [Header("Cached Components")]

    [HideInInspector]
    public Transform SelfTransform;

    [Header("Components")]

    public BaseProjectileTick Tick;
    public GameObject Model;

    [HideInInspector]
    public Teams OwningPlayerTeam;

    public float Damage;
    public LayerMask PlayerLayer;
    public LayerMask PlayerObjectLayer;

    public virtual void Awake()
    {
        SelfTransform = transform;
        Tick.enabled = false;
    }

    public virtual void Start()
    {
        Tick.bIsServer = IsServer;
    }

    public virtual void OnHitGround() { }

    public virtual void OnHitPlayer() { }

    public virtual void OnHitPlayerWithTarget(PlayerManager player) { }

    public virtual void Init(Teams team, Vector3 pos, Vector3 dir)
    {
        InitClientRpc(team, pos, dir);
        OwningPlayerTeam = team;
        SelfTransform.position = pos;
        SelfTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        Tick.LastTimeReplicatedPosition = Time.time;
        Tick.StartTime = Tick.TimeStamp;
        Tick.Velocity = dir * Tick.InitialSpeed;
    }

    public virtual void InitAndSimulateForward(Teams team, Vector3 pos, Vector3 dir, int tickstosimulate)
    {
        OwningPlayerTeam = team;
        SelfTransform.position = pos;
        SelfTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        for(int i = 0; i < tickstosimulate; i++)
        {
            Tick.FixedUpdate();
        }

        ReplicatePositionClientRpc(SelfTransform.position);

        Tick.Velocity = dir * Tick.InitialSpeed;
        Tick.LastTimeReplicatedPosition = Time.time;
        Tick.StartTime = Tick.TimeStamp;
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
    public void ReplicatePositionClientRpc(Vector3 pos)
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

        OwningPlayerTeam = team;
        SelfTransform.position = pos;
        SelfTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        Tick.bUpdatedThisFrame = true;
        Tick.StartTime = Tick.TimeStamp;
        Tick.Velocity = dir * Tick.InitialSpeed;
    }
}
