using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.VisualScripting;

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

    [Header("Components")]

    public List<MonoBehaviour> DisabledForOwnerScripts;
    public List<MonoBehaviour> DisabledForOthersScripts;

    public Camera PlayerCamera;
    public AudioListener PlayerAudioListener;

    public GameObject FirstPersonComponents;
    public GameObject ThirdPersonComponents;

    [Header("Hit Registration")]

    private Dictionary<int, Vector3> RewindDataDictionary = new Dictionary<int, Vector3>();
    private Vector3 OriginalPosition;

    [Header("Stats")]

    public int MaxHealth;
    private NetworkVariable<float> Health = new NetworkVariable<float>();

    [SerializeField]
    private TMP_Text HealthBarText;

    private Teams Team;

    private bool Dead;

    // Start is called before the first frame update
    void Start()
    {
        if (IsServer && !IsOwner)
        {
            TimeStamp = -ServerDelay;

            OwningClientID = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            };
        }

        List<PlayerInformation> PlayerList = GameManager.Singleton.GetAllPlayerInformation();

        foreach (PlayerInformation playerInfo in PlayerList)
        {
            if (playerInfo.Id == OwnerClientId)
            {
                Team = playerInfo.Team;
            }
        }

        if (IsServer)
        {
            Health.Value = MaxHealth;
        }

        OnHealthChanged(Health.Value, Health.Value);

        if (Team == Teams.Red)
        {
            HealthBarText.color = Color.red;

            return;
        }

        if (Team == Teams.Blue)
        {
            HealthBarText.color = Color.blue;

            return;
        }
    }

    public override void OnNetworkSpawn()
    {
        Health.OnValueChanged += OnHealthChanged;

        if (IsOwner)
        {
            foreach (var i in DisabledForOwnerScripts)
            {
                i.enabled = false;
            }

            ThirdPersonComponents.SetActive(false);

            return;
        }

        foreach (var i in DisabledForOthersScripts)
        {
            i.enabled = false;
        }

        FirstPersonComponents.SetActive(false);

        PlayerCamera.enabled = false;
        PlayerAudioListener.enabled = false;
    }

    public override void OnNetworkDespawn()
    {
        Health.OnValueChanged -= OnHealthChanged;
    }

    public void FixedUpdate()
    {
        TimeStamp++;

        if(!IsServer)
        {
            return;
        }

        RewindDataDictionary.Add(TimeStamp, transform.position);

        if(transform.position.y < -100)
        {
            Health.Value = -1;
        }
    }

    public bool RewindToPosition(Teams team, int pingintick)
    {
        if(Team == team)
        {
            return false;
        }

        int RewindToTime = TimeStamp - (ServerDelay + pingintick);

        if (RewindDataDictionary.TryGetValue(RewindToTime, out Vector3 RewindedPosition))
        {
            OriginalPosition = transform.position;
            transform.position = RewindedPosition;

            return true;
        }

        return false;
    }

    public void ResetToOriginalPosition()
    {
        if(Dead)
        {
            return;
        }

        transform.position = OriginalPosition;
    }

    public int GetPingInTick()
    {
        ulong ping = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(OwnerClientId);

        int pingintick = (int)(((float)(ping)) * 0.04f);

        print("PING:" + pingintick);

        return pingintick;
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

    public void Damage(Teams team, float damage)
    {
        if (!IsServer || Team == team)
        {
            return;
        }

        Health.Value -= damage;
    }

    public void OnHealthChanged(float previous, float current)
    {
        HealthBarText.text = current.ToString() + " / " + MaxHealth.ToString();

        if (previous > 0 && current <= 0)
        {
            Dead = true;

            if (!IsServer)
            {
                Invoke(nameof(DieOnClient), 0.5f);

                return;
            }

            transform.position = GameManager.Singleton.GetGraveyardLocation();

            Invoke(nameof(Respawn), 7);

            return;
        }

        if(previous <= 0 && current > 0)
        {
            Dead = false;

            transform.position = GameManager.Singleton.GetSpawnLocation(Team);
        }
    }

    public void DieOnClient()
    {
        transform.position = GameManager.Singleton.GetGraveyardLocation();
    }

    public void Respawn()
    {
        Health.Value = MaxHealth;
    }

    public int GetTimeStamp()
    {
        return TimeStamp;
    }

    public bool GetIsDead()
    {
        return Dead;
    }

    public Teams GetTeam()
    {
        return Team;
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

        if (IsOwner)
        {
            return NetworkRole.AutonomousProxy;
        }

        return NetworkRole.SimulatedProxy;
    }
}
