using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum NetworkRole
{
    AutonomousProxy,
    SimulatedProxy,
    HostOwner,
    HostProxy
}

public class PlayerManager : NetworkBehaviour
{
    [Header("Ticking")]

    public int ServerDelay = 4;

    private int TimeStamp;

    [Header("Networking")]

    private ClientRpcParams OwningClientID;

    private int TotalTimes;
    private int TotalTimeDifference;
    private float LastTimeSentClientTimeCorrection;

    private void Start()
    {
        if (IsServer && !IsOwner)
        {
            TimeStamp = -GetServerDelay();

            OwningClientID = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            };
        }
    }

    public ClientRpcParams GetClientRpcParamsSendToOwner()
    {
        return OwningClientID;
    }

    public NetworkRole GetLocalRole()
    {
        if (IsServer)
        {
            if (IsOwner)
            {
                return NetworkRole.HostOwner;
            }

            return NetworkRole.HostProxy;
        }

        if(IsOwner)
        {
            return NetworkRole.AutonomousProxy;
        }

        return NetworkRole.SimulatedProxy;
    }

    public void CheckClientTimeError(int clienttime)
    {
        if (Time.time - LastTimeSentClientTimeCorrection < 1)
        {
            return;
        }

        TotalTimes++;

        int CorrectTime = TimeStamp + ServerDelay;

        TotalTimeDifference = TotalTimeDifference + CorrectTime - clienttime;

        if (Time.time - LastTimeSentClientTimeCorrection > 5)
        {
            LastTimeSentClientTimeCorrection = Time.time;

            int timediff = TotalTimeDifference / TotalTimes;

            if (timediff != 0)
            {
                SendClientTimeCorrectionClientRpc(timediff, OwningClientID);

                print(TotalTimeDifference / TotalTimes);
            }

            TotalTimeDifference = 0;
            TotalTimes = 0;
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void SendClientTimeCorrectionClientRpc(int timediff, ClientRpcParams clientRpcParams = default)
    {
        TimeStamp += timediff;
    }

    public void FixedUpdate()
    {
        TimeStamp++;
    }

    public int GetTimeStamp()
    {
        return TimeStamp;
    }

    public int GetServerDelay()
    {
        return TimeStamp;
    }
}
