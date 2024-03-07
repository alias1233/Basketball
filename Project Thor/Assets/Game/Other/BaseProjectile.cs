using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BaseProjectile : NetworkBehaviour
{
    [Header("Cached Components")]

    public Transform SelfTransform;

    public GameObject Model;

    public float InitialSpeed;
    public bool bGravity;
    public float Gravity;
    public bool bBounce;
    public int Bounces;

    private Vector3 Velocity;

    [HideInInspector]
    public Teams OwningPlayerTeam;

    public float Lifetime;

    private float StartTime;

    public float ReplicatePositionInterval;

    private float LastTimeReplicatedPosition;
    private bool bUpdatedThisFrame;

    public float Damage;
    public LayerMask PlayerLayer;

    public virtual void Awake()
    {
        SelfTransform = transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Model.activeSelf)
        {
            return;
        }

        if (IsServer)
        {
            SelfTransform.position += Velocity * Time.deltaTime;

            if(Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
            {
                LastTimeReplicatedPosition = Time.time;

                ReplicatePositionClientRpc(SelfTransform.position);
            }

            if(Time.time - StartTime >= Lifetime)
            {
                ReplicateDisableClientRpc();
                gameObject.SetActive(false);
            }

            return;
        }

        if(bUpdatedThisFrame)
        {
            bUpdatedThisFrame = false;

            return;
        }

        SelfTransform.position += Velocity * Time.deltaTime;
    }

    public virtual void OnHitGround() { }

    public virtual void OnHitPlayer() { }

    private void OnTriggerEnter(Collider other)
    {
        if(!IsServer || !Model.activeSelf)
        {
            return;
        }

        if (other.gameObject.layer == 0)
        {
            OnHitGround();

            return;
        }

        if(other.gameObject.TryGetComponent<PlayerManager>(out PlayerManager player))
        {
            if(OwningPlayerTeam != player.GetTeam())
            {
                OnHitPlayer();
            }
        }
    }

    public void DisableGameObject()
    {
        gameObject.SetActive(false);
    }

    public void Init(Teams team, Vector3 pos, Quaternion dir)
    {
        Model.SetActive(true);

        LastTimeReplicatedPosition = StartTime;
        InitClientRpc(team, pos, dir);
        StartTime = Time.time;

        OwningPlayerTeam = team;
        SelfTransform.position = pos;
        Velocity = dir * Vector3.forward * InitialSpeed;
        SelfTransform.rotation = dir;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void ReplicateDisableClientRpc()
    {
        if (IsServer)
        {
            return;
        }

        Model.SetActive(false);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void ReplicatePositionClientRpc(Vector3 pos)
    {
        if(IsServer)
        {
            return;
        }

        if (!Model.activeSelf)
        {
            Model.SetActive(true);
        }

        bUpdatedThisFrame = true;

        SelfTransform.position = pos;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void InitClientRpc(Teams team, Vector3 pos, Quaternion dir)
    {
        if (IsServer)
        {
            return;
        }

        if (!Model.activeSelf)
        {
            Model.SetActive(true);
        }

        bUpdatedThisFrame = true;

        OwningPlayerTeam = team;
        SelfTransform.position = pos;
        Velocity = dir * Vector3.forward * InitialSpeed;
        SelfTransform.rotation = dir;
    }
}
