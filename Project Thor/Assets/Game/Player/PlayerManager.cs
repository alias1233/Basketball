using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using TMPro;

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

    [SerializeField]
    private GameObject FirstPersonComponents;
    [SerializeField]
    private GameObject ThirdPersonComponents;

    [SerializeField]
    private GameObject FPPlayerCamera;
    [SerializeField]
    private CameraVisualsScript CameraVisuals;
    [SerializeField]
    private GameObject FirstPersonPlayerUI;

    public GameObject[] RenderOnTop;

    [Header("Hit Registration")]

    private Dictionary<int, Vector3> RewindDataDictionary = new Dictionary<int, Vector3>();
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

    public AudioSource DeathSound;

    // Start is called before the first frame update
    void Start()
    {
        Movement = GetComponent<PlayerMovement>();
        Weapons = GetComponent<WeaponManager>();

        List<PlayerInformation> PlayerList = GameManager.Singleton.GetAllPlayerInformation();

        foreach (PlayerInformation playerInfo in PlayerList)
        {
            if (playerInfo.Id == OwnerClientId)
            {
                Team = playerInfo.Team;

                break;
            }
        }

        if (IsOwner)
        {
            if(!IsServer)
            {
                OnHealthChanged(Health.Value, Health.Value);
            }

            if (Team == Teams.Red)
            {
                FirstPersonHealthBarText.color = Color.red;
            }

            else if (Team == Teams.Blue)
            {
                FirstPersonHealthBarText.color = Color.blue;
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
            TimeStamp = -ServerDelay;

            OwningClientID = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            };

            Health.Value = MaxHealth;

            OnHealthChanged(Health.Value, Health.Value);
        }

        Health.OnValueChanged += OnHealthChanged;

        if (IsOwner)
        {
            foreach (var i in DisabledForOwnerScripts)
            {
                i.enabled = false;
            }

            ThirdPersonComponents.SetActive(false);

            foreach(var i in RenderOnTop)
            {
                SetGameLayerRecursive(i, 6);
            }

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

        if(Dead)
        {
            return;
        }

        Movement.FixedTick(TimeStamp);
        Weapons.FixedTick(TimeStamp);

        if(!IsServer)
        {
            return;
        }

        RewindDataDictionary.Add(TimeStamp, transform.position);
        RewindDataDictionary.Remove(TimeStamp - 40);

        if(transform.position.y < -10)
        {
            Health.Value = -1;
        }
    }

    public void OnHealthChanged(float previous, float current)
    {
        if (IsOwner)
        {
            FirstPersonHealthBarText.text = ((int)current).ToString();
        }

        else
        {
            ThirdPersonHealthBarText.text = ((int)current).ToString();
        }

        if (previous > 0 && current <= 0)
        {
            Die();

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
            Movement.ChangeVelocity(Vector3.zero);
        }
    }

    private void Die()
    {
        Dead = true;

        DeathSound.Play();

        if (IsOwner)
        {
            DeathManager.Singleton.PossessGhost(transform.position, FPPlayerCamera.transform.rotation);

            FPPlayerCamera.SetActive(false);
            FirstPersonPlayerUI.SetActive(false);
        }

        if (IsServer)
        {
            transform.position = GameManager.Singleton.GetGraveyardLocation();

            Invoke(nameof(Respawn), RespawnTime);

            RewindDataDictionary.Clear();

            return;
        }

        Invoke(nameof(DieOnClient), 0.5f);
    }

    public bool Damage(Teams team, float damage)
    {
        if (Team == team || !IsServer)
        {
            return false;
        }

        Health.Value -= damage;

        return true;
    }

    public void DieOnClient()
    {
        if(Health.Value < 0)
        {
            transform.position = GameManager.Singleton.GetGraveyardLocation();
        }
    }

    public void Respawn()
    {
        Health.Value = MaxHealth;
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
        return (int)(NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(OwnerClientId) * 0.05f);
    }

    public void CheckClientTimeError(int clienttime)
    {
        if (Time.time - LastTimeSentClientTimeCorrection < 1)
        {
            return;
        }

        TotalTimes++;

        TotalTimeDifference = TotalTimeDifference + TimeStamp + ServerDelay - clienttime;

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

    public bool IsSameTeam(Teams team)
    {
        return Team == team;
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

    public ulong GetClientID()
    {
        return OwnerClientId;
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

    private void SetGameLayerRecursive(GameObject gameObject, int layer)
    {
        var children = gameObject.GetComponentsInChildren<Transform>(includeInactive: true);

        foreach (var child in children)
        {
            child.gameObject.layer = layer;
        }
    }
}
