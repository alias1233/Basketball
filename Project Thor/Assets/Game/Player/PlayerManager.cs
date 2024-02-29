using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;
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

    public int ServerDelay = 5;

    private int TimeStamp;

    [Header("Networking")]

    private ClientRpcParams OwningClientID;

    private int TotalTimes;
    private int TotalTimeDifference;
    private float LastTimeSentClientTimeCorrection;

    [Header("Components")]

    private PlayerMovement Movement;
    private WeaponManager Weapons;

    public List<MonoBehaviour> DisabledForOwnerScripts;
    public List<MonoBehaviour> DisabledForOthersScripts;

    public GameObject FirstPersonComponents;
    public GameObject ThirdPersonComponents;

    public GameObject FPPlayerCamera;
    public CameraVisualsScript CameraVisuals;
    public GameObject FirstPersonPlayerUI;

    [Header("Hit Registration")]

    private Dictionary<int, Vector3> RewindDataDictionary = new Dictionary<int, Vector3>();
    int[] Keys = new int[100];
    private Vector3 OriginalPosition;

    [Header("Stats")]

    private Teams Team;

    public int MaxHealth;
    private NetworkVariable<float> Health = new NetworkVariable<float>();

    public int RespawnTime;

    private bool Dead;

    [SerializeField]
    private TMP_Text FirstPersonHealthBarText;
    [SerializeField]
    private TMP_Text ThirdPersonHealthBarText;

    // Start is called before the first frame update
    void Start()
    {
        Movement = GetComponent<PlayerMovement>();
        Weapons = GetComponent<WeaponManager>();

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
        FPPlayerCamera.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
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

        if(TimeStamp % 40 == 0)
        {
            RewindDataDictionary.Keys.CopyTo(Keys, 0);
            int Threshold = TimeStamp - 40;

            foreach (int i in Keys)
            {
                if(i < Threshold)
                {
                    RewindDataDictionary.Remove(i);
                }
            }
        }

        if(transform.position.y < -100)
        {
            Health.Value = -1;
        }
    }

    public void OnHealthChanged(float previous, float current)
    {
        if (IsOwner)
        {
            FirstPersonHealthBarText.text = ((int)(current)).ToString() + " / " + MaxHealth.ToString();
        }

        else
        {
            ThirdPersonHealthBarText.text = ((int)(current)).ToString() + " / " + MaxHealth.ToString();
        }

        if (previous > 0 && current <= 0)
        {
            Dead = true;

            if(IsOwner)
            {
                DeathManager.Singleton.PossessGhost(transform.position, FPPlayerCamera.transform.rotation);

                FPPlayerCamera.SetActive(false);
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

                FPPlayerCamera.SetActive(true);
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

    public void CameraChangePosition(Vector3 Offset, float duration)
    {
        CameraVisuals.ChangePosition(Offset, duration);
    }
    public void CameraResetPosition(float duration)
    {
        CameraVisuals.ResetPosition(duration);
    }

    public void ChangeFOV(float FOVdiff, float duration)
    {
        CameraVisuals.ChangeFOV(FOVdiff, duration);
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
