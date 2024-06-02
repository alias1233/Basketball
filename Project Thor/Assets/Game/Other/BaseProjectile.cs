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
        ProjectileCollider.enabled = true;
        Tick.enabled = true;
    }

    virtual public void Deactivate()
    {
        Model.SetActive(false);
        ProjectileCollider.enabled = false;
        Tick.enabled = false;
    }

    [Header("Cached Components")]

    [HideInInspector]
    public Transform SelfTransform;

    private Collider ProjectileCollider;

    [Header("Components")]

    public BaseProjectileTick Tick;
    public GameObject Model;

    [HideInInspector]
    public BasePlayerManager OwningPlayer;
    [HideInInspector]
    public Teams OwningPlayerTeam;

    public float Damage;
    public LayerMask ObjectLayer;
    public LayerMask PlayerLayer;
    public LayerMask PlayerObjectLayer;

    public virtual void Awake()
    {
        SelfTransform = transform;
        ProjectileCollider = GetComponent<Collider>();
    }

    public virtual void Start()
    {

    }

    public override void OnNetworkSpawn()
    {
        Tick.bIsServer = IsServer;

        Despawn();
    }

    public virtual void OnHitGround() { }

    public virtual void OnHitPlayer() { }

    public virtual void OnHitPlayerWithTarget(BasePlayerManager player) { }

    public virtual void Trigger() { }

    public virtual void Init(BasePlayerManager Player, Vector3 pos, Vector3 dir)
    {
        OwningPlayer = Player;
        OwningPlayerTeam = OwningPlayer.GetTeam();
        InitClientRpc(OwningPlayerTeam, pos, dir);
        SelfTransform.position = pos;
        SelfTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        Tick.LastTimeReplicatedPosition = Time.time;
        Tick.OwningPlayerTeam = OwningPlayerTeam;
        Tick.StartTime = Tick.TimeStamp;
        Tick.Velocity = dir * Tick.InitialSpeed;
    }

    public virtual void InitNoRot(BasePlayerManager Player, Vector3 pos, Vector3 dir)
    {
        OwningPlayer = Player;
        OwningPlayerTeam = OwningPlayer.GetTeam();
        InitNoRotClientRpc(OwningPlayerTeam, pos, dir);
        SelfTransform.position = pos;

        Tick.LastTimeReplicatedPosition = Time.time;
        Tick.OwningPlayerTeam = OwningPlayerTeam;
        Tick.StartTime = Tick.TimeStamp;
        Tick.Velocity = dir * Tick.InitialSpeed;
    }

    public virtual void InitAndSimulateForward(BasePlayerManager Player, Vector3 pos, Vector3 dir, int tickstosimulate)
    {
        OwningPlayer = Player;
        OwningPlayerTeam = OwningPlayer.GetTeam();
        SelfTransform.position = pos;
        SelfTransform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        Tick.Velocity = dir * Tick.InitialSpeed;
        Tick.OwningPlayerTeam = OwningPlayerTeam;
        Tick.LastTimeReplicatedPosition = Time.time;
        Tick.StartTime = Tick.TimeStamp;

        for (int i = 0; i < tickstosimulate; i++)
        {
            Tick.FixedUpdate();
        }

        ReplicatePositionClientRpc(SelfTransform.position);
    }

    public virtual void InitStationary(BasePlayerManager Player, Vector3 pos)
    {
        OwningPlayer = Player;
        OwningPlayerTeam = OwningPlayer.GetTeam();
        InitStationaryClientRpc(OwningPlayerTeam, pos);
        SelfTransform.position = pos;

        Tick.LastTimeReplicatedPosition = Time.time;
        Tick.OwningPlayerTeam = OwningPlayerTeam;
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

        SelfTransform.position = pos;
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
        Tick.OwningPlayerTeam = team;
        Tick.Velocity = dir * Tick.InitialSpeed;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void InitNoRotClientRpc(Teams team, Vector3 pos, Vector3 dir)
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

        Tick.bUpdatedThisFrame = true;
        Tick.StartTime = Tick.TimeStamp;
        Tick.OwningPlayerTeam = team;
        Tick.Velocity = dir * Tick.InitialSpeed;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void InitStationaryClientRpc(Teams team, Vector3 pos)
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

        Tick.bUpdatedThisFrame = true;
        Tick.StartTime = Tick.TimeStamp;
        Tick.OwningPlayerTeam = team;
    }
}
