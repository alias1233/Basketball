using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
using Unity.VisualScripting;
using static ConnectionNotificationManager;

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
    private ClientRpcParams IgnoreOwnerRPCParams;

    private int TotalTimes;
    private int TotalTimeDifference;
    private float LastTimeSentClientTimeCorrection;

    [Header("Components")]

    public PlayerMovement Movement;
    public WeaponManager Weapons;

    public List<MonoBehaviour> DisabledForOwnerScripts;
    public List<MonoBehaviour> DisabledForOthersScripts;

    public Camera PlayerCamera;
    public AudioListener PlayerAudioListener;

    public GameObject FirstPersonComponents;
    public GameObject ThirdPersonComponents;

    public GameObject FirstPersonPlayerUI;

    [Header("Hit Registration")]

    private Dictionary<int, Vector3> RewindDataDictionary = new Dictionary<int, Vector3>();
    private Vector3 OriginalPosition;

    [Header("Stats")]

    public int MaxHealth;
    private NetworkVariable<float> Health = new NetworkVariable<float>();

    [SerializeField]
    private TMP_Text FirstPersonHealthBarText;
    [SerializeField]
    private TMP_Text ThirdPersonHealthBarText;

    private Teams Team;

    private bool Dead;

    public int RespawnTime;

    // Start is called before the first frame update
    void Start()
    {
        if (IsServer)
        {
            TimeStamp = -ServerDelay;

            OwningClientID = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            };

            List<ulong> ClientIDList = new List<ulong>();

            foreach(ulong i in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if(i != OwnerClientId && i != NetworkManager.ServerClientId)
                {
                    ClientIDList.Add(i);
                }
            }

            IgnoreOwnerRPCParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ClientIDList
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

        if (IsOwner)
        {
            if (Team == Teams.Red)
            {
                FirstPersonHealthBarText.color = Color.red;

                return;
            }

            if (Team == Teams.Blue)
            {
                FirstPersonHealthBarText.color = Color.blue;

                return;
            }

            return;
        }

        if (Team == Teams.Red)
        {
            ThirdPersonHealthBarText.color = Color.red;

            return;
        }

        if (Team == Teams.Blue)
        {
            ThirdPersonHealthBarText.color = Color.blue;

            return;
        }
    }

    public override void OnNetworkSpawn()
    {
        if(IsServer)
        {
            ConnectionNotificationManager.Singleton.OnClientConnectionNotification += UpdateClientSendRPCParams;
        }

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
        if(IsServer)
        {
            ConnectionNotificationManager.Singleton.OnClientConnectionNotification -= UpdateClientSendRPCParams;
        }

        Health.OnValueChanged -= OnHealthChanged;
    }

    void FixedUpdate()
    {
        TimeStamp++;

        if(!Dead)
        {
            Movement.FixedTick(TimeStamp);
            Weapons.FixedTick(TimeStamp);
        }

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

    private void UpdateClientSendRPCParams(ulong clientId, ConnectionStatus connection)
    {
        List<ulong> ClientIDList = new List<ulong>();

        foreach (ulong i in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (i != OwnerClientId && i != NetworkManager.ServerClientId)
            {
                ClientIDList.Add(i);
            }
        }

        IgnoreOwnerRPCParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = ClientIDList
            }
        };
    }

    public void OnHealthChanged(float previous, float current)
    {
        if (IsOwner)
        {
            FirstPersonHealthBarText.text = current.ToString() + " / " + MaxHealth.ToString();
        }

        else
        {
            ThirdPersonHealthBarText.text = current.ToString() + " / " + MaxHealth.ToString();
        }

        if (previous > 0 && current <= 0)
        {
            Dead = true;

            if(IsOwner)
            {
                DeathManager.Singleton.PossessGhost(transform.position, PlayerCamera.transform.rotation);

                PlayerCamera.enabled = false;
                PlayerAudioListener.enabled = false;
                FirstPersonPlayerUI.SetActive(false);
            }

            if (!IsServer)
            {
                Invoke(nameof(DieOnClient), 0.5f);

                return;
            }

            transform.position = GameManager.Singleton.GetGraveyardLocation();

            Invoke(nameof(Respawn), RespawnTime);

            return;
        }

        if(previous <= 0 && current > 0)
        {
            Dead = false;

            if (IsOwner)
            {
                DeathManager.Singleton.UnpossessGhost();

                PlayerCamera.enabled = true;
                PlayerAudioListener.enabled = true;
                FirstPersonPlayerUI.SetActive(true);
            }

            transform.position = GameManager.Singleton.GetSpawnLocation(Team);
        }
    }

    public bool RewindToPosition(Teams team, int pingintick)
    {
        if (Team == team)
        {
            return false;
        }

        int RewindToTime = TimeStamp - (ServerDelay + pingintick);

        if (RewindDataDictionary.TryGetValue(RewindToTime, out Vector3 RewindedPosition))
        {
            OriginalPosition = transform.position;
            transform.position = RewindedPosition;

            //print("ORIGINAL POSITION AT " + TimeStamp + ": " + OriginalPosition);
            //print("REWINDED POSITION " + TimeStamp + ": " + transform.position);

            return true;
        }

        return false;
    }

    public void ResetToOriginalPosition()
    {
        if (Dead)
        {
            return;
        }

        transform.position = OriginalPosition;
    }

    public int GetPingInTick()
    {
        ulong ping = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(OwnerClientId);

        int pingintick = (int)((float)ping * 0.04f);

        print("PING:" + ping);

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

    public ClientRpcParams GetClientRpcParamsIgnoreOwner()
    {
        return IgnoreOwnerRPCParams;
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
