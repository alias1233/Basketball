using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class BaseProjectile : NetworkBehaviour
{
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

    // Update is called once per frame
    void Update()
    {
        if (!Model.activeSelf)
        {
            return;
        }

        if (IsServer)
        {
            transform.Translate(Velocity * Time.deltaTime);

            if(Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
            {
                LastTimeReplicatedPosition = Time.time;

                ReplicatePositionClientRpc(transform.position);
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

        transform.Translate(Velocity * Time.deltaTime);
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

    public void Init(Teams team, Vector3 pos, Vector3 dir)
    {
        Model.SetActive(true);

        LastTimeReplicatedPosition = StartTime;
        InitClientRpc(team, pos, dir);
        StartTime = Time.time;

        OwningPlayerTeam = team;
        transform.position = pos;
        Velocity = dir * InitialSpeed;
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

        transform.position = pos;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void InitClientRpc(Teams team, Vector3 pos, Vector3 dir)
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
        transform.position = pos;
        Velocity = dir * InitialSpeed;
    }
}
