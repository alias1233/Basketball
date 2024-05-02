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

public class BasePlayerManager : NetworkBehaviour
{
    protected Transform SelfTransform;

    public BaseCharacterComponents Components;

    [Header("Ticking")]

    public int ServerDelay = 3;

    protected int TimeStamp;

    [Header("Networking")]

    private ClientRpcParams OwningClientID;
    private List<ulong> ClientIDList = new List<ulong>();

    private int TotalTimes;
    private int TotalTimeDifference;
    private float LastTimeSentClientTimeCorrection;

    [Header("Components")]

    [HideInInspector]
    public BaseCharacterMovement Movement;
    protected WeaponManager Weapons;

    protected GameObject CharacterModel;
    protected BaseCharacterModelAnimScript CharacterAnimations;

    private Vector3 CharacterModelOriginalPosition;
    private Quaternion CharacterModelOriginalRotation;

    protected Transform HandTransform;
    protected Transform ThrowBallLocationTransform;

    protected GameObject FirstPersonComponents;
    protected GameObject ThirdPersonComponents;

    protected GameObject FPPlayerCamera;
    protected GameObject FirstPersonPlayerUI;

    protected CameraScript camerascript;

    [Header("Hit Registration")]

    private Dictionary<int, Vector3> RewindDataDictionary = new Dictionary<int, Vector3>();
    private Vector3 OriginalPosition;

    [Header("Stats")]

    protected Teams Team;

    public int MaxHealth;
    private NetworkVariable<float> Health = new NetworkVariable<float>();

    public float TimeBeforeBeginHealing = 7;
    public float TimeBetweenHealing = 1;
    public float HealAmount;

    private float LastTimeDamaged;
    private float LastTimeHealed;

    public int RespawnTime = 7;

    private bool Dead;

    protected ProgressBar FirstPersonHealthBar;
    protected TMP_Text FirstPersonHealthBarText;
    protected GameObject ThirdPersonHealthBarObject;
    protected ProgressBar ThirdPersonHealthBar;

    protected AudioSource DeathSound;

    protected virtual void Awake()
    {
        SelfTransform = transform;

        Movement = GetComponent<BaseCharacterMovement>();
        Weapons = GetComponent<WeaponManager>();
        CharacterModel = Components.CharacterModel;
        CharacterAnimations = Components.CharacterAnimations;
        HandTransform = Components.HandTransform;
        ThrowBallLocationTransform = Components.ThrowBallLocationTransform;
        FirstPersonComponents = Components.FirstPersonComponents;
        ThirdPersonComponents = Components.ThirdPersonComponents;
        FPPlayerCamera = Components.FPPlayerCamera;
        camerascript = Components.camerascript;
        FirstPersonPlayerUI = Components.FirstPersonPlayerUI;
        FirstPersonHealthBar = Components.FirstPersonHealthBar;
        FirstPersonHealthBarText = Components.FirstPersonHealthBarText;
        ThirdPersonHealthBarObject = Components.ThirdPersonHealthBarObject;
        ThirdPersonHealthBar = Components.ThirdPersonHealthBar;
        DeathSound = Components.DeathSound;

        CharacterModelOriginalPosition = CharacterModel.transform.localPosition;
        CharacterModelOriginalRotation = CharacterModel.transform.localRotation;
    }

    // Start is called before the first frame update
    private void Start()
    {
        List<PlayerInformation> PlayerList = GameManager.Singleton.GetAllPlayerInformation();

        bool bfound = false;

        foreach (PlayerInformation playerInfo in PlayerList)
        {
            if (playerInfo.Id == OwnerClientId)
            {
                Team = playerInfo.Team;

                bfound = true;

                break;
            }
        }

        if (!bfound)
        {
            Invoke(nameof(TryInitAgain), 1);
        }

        if (!IsServer)
        {
            OnHealthChanged(MaxHealth, MaxHealth);
        }

        if (IsOwner)
        {
            if (Team == Teams.Red)
            {
                FirstPersonHealthBar.GetFillImage().color = Color.red;
            }

            else if (Team == Teams.Blue)
            {
                FirstPersonHealthBar.GetFillImage().color = Color.blue;
            }

            return;
        }

        if (Team == Teams.Red)
        {
            ThirdPersonHealthBar.GetFillImage().color = Color.red;

            return;
        }

        if (Team == Teams.Blue)
        {
            ThirdPersonHealthBar.GetFillImage().color = Color.blue;

            return;
        }
    }

    public override void OnNetworkSpawn()
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

            ConnectionNotificationManager.Singleton.OnClientConnectionNotification += UpdateClientSendRPCParams;

            foreach (ulong i in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (i != OwnerClientId && i != 0)
                {
                    ClientIDList.Add(i);
                }
            }

            ClientRpcParams IgnoreOwnerRPCParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ClientIDList
                }
            };

            Movement.UpdateIgnoreOwnerRPCParams(IgnoreOwnerRPCParams);
            Weapons.UpdateIgnoreOwnerRPCParams(IgnoreOwnerRPCParams);

            Health.Value = MaxHealth;

            OnHealthChanged(Health.Value, Health.Value);
        }

        Health.OnValueChanged += OnHealthChanged;

        if (IsOwner)
        {
            SettingsUIScript.Singleton.OnSettingsUIChangeActive += OnChangeActiveSettingsUI;

            camerascript.Sens = SettingsUIScript.Singleton.GetSensitivity();

            foreach (var i in Components.DisabledForOwnerScripts)
            {
                i.enabled = false;
            }

            ThirdPersonComponents.SetActive(false);

            foreach (var i in Components.RenderOnTop)
            {
                SetGameLayerRecursive(i, 6);
            }

            ThirdPersonHealthBarObject.SetActive(false);

            Ball.Singleton.SetOwningPlayer(SelfTransform);

            return;
        }

        foreach (var i in Components.DisabledForOthersScripts)
        {
            i.enabled = false;
        }

        FirstPersonComponents.SetActive(false);
        FPPlayerCamera.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        Health.OnValueChanged -= OnHealthChanged;
        ConnectionNotificationManager.Singleton.OnClientConnectionNotification -= UpdateClientSendRPCParams;
        SettingsUIScript.Singleton.OnSettingsUIChangeActive -= OnChangeActiveSettingsUI;

        if (GetIsHoldingBall())
        {
            Ball.Singleton.Detach();
        }
    }

    private void OnChangeActiveSettingsUI(bool active)
    {
        if(active)
        {
            FirstPersonPlayerUI.SetActive(false);
            camerascript.enabled = false;

            return;
        }

        camerascript.Sens = SettingsUIScript.Singleton.GetSensitivity();
        FirstPersonPlayerUI.SetActive(true);
        camerascript.enabled = true;
    }

    private void UpdateClientSendRPCParams(ulong clientId, ConnectionStatus connection)
    {
        if (connection == ConnectionStatus.Connected)
        {
            ClientIDList.Add(clientId);
        }

        else
        {
            ClientIDList.Remove(clientId);
        }

        ClientRpcParams IgnoreOwnerRPCParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = ClientIDList
            }
        };

        Movement.UpdateIgnoreOwnerRPCParams(IgnoreOwnerRPCParams);
        Weapons.UpdateIgnoreOwnerRPCParams(IgnoreOwnerRPCParams);
    }

    void FixedUpdate()
    {
        TimeStamp++;

        if (Dead)
        {
            return;
        }

        Movement.FixedTick(TimeStamp);
        Weapons.FixedTick(TimeStamp);

        if (!IsServer)
        {
            return;
        }

        HandleRegeneration();

        RewindDataDictionary.Add(TimeStamp, SelfTransform.position);
        RewindDataDictionary.Remove(TimeStamp - 40);

        if (SelfTransform.position.y < -20)
        {
            Health.Value = -1;
        }
    }

    public void OnHealthChanged(float previous, float current)
    {
        if (previous > 0 && current <= 0)
        {
            Die();

            if (IsOwner)
            {
                FirstPersonHealthBarText.text = "0";
                FirstPersonHealthBar.UpdateProgressBar(0);
            }

            else
            {
                ThirdPersonHealthBar.UpdateProgressBar(0);
            }

            return;
        }

        if (previous <= 0 && current > 0)
        {
            Respawn();
        }

        if (IsOwner)
        {
            FirstPersonHealthBarText.text = ((int)current).ToString();
            FirstPersonHealthBar.UpdateProgressBar(current / MaxHealth);
        }

        else
        {
            ThirdPersonHealthBar.UpdateProgressBar(current / MaxHealth);
        }
    }

    private void Die()
    {
        Dead = true;

        CharacterAnimations.Die();
        DeathSound.Play();

        if (Weapons.bHoldingBall)
        {
            Ball.Singleton.Detach();
        }

        Movement.ResetMovement();

        if (IsServer)
        {
            Invoke(nameof(ResetHealth), RespawnTime);

            RewindDataDictionary.Clear();
        }

        if (IsOwner)
        {
            DeathManager.Singleton.PossessGhost(SelfTransform.position, FPPlayerCamera.transform.rotation);
            DeathManager.Singleton.SetRespawnTime(RespawnTime);

            FPPlayerCamera.SetActive(false);
            FirstPersonPlayerUI.SetActive(false);

            SendToGraveyard();

            return;
        }

        Invoke(nameof(SendToGraveyard), 1.5f);
    }

    private void ResetHealth()
    {
        Health.Value = MaxHealth;
    }

    private void Respawn()
    {
        Dead = false;

        if (IsOwner)
        {
            DeathManager.Singleton.UnpossessGhost();

            FPPlayerCamera.SetActive(true);
            FirstPersonPlayerUI.SetActive(true);
        }

        SelfTransform.position = GameManager.Singleton.GetSpawnLocation(Team);
        Movement.ChangeVelocity(Vector3.zero);
    }

    public bool Damage(Teams team, float damage)
    {
        if (Team == team || !IsServer)
        {
            return false;
        }

        Health.Value -= damage;

        LastTimeDamaged = Time.time;

        return true;
    }

    public bool DamageWithKnockback(Teams team, float damage,
        Vector3 Impulse, bool bExternalSource)
    {
        Movement.AddVelocity(Impulse, bExternalSource);

        if (Team == team || !IsServer)
        {
            return false;
        }

        Health.Value -= damage;

        LastTimeDamaged = Time.time;

        return true;
    }

    private void HandleRegeneration()
    {
        if (Time.time - LastTimeDamaged < TimeBeforeBeginHealing)
        {
            return;
        }

        if (Time.time - LastTimeHealed > TimeBetweenHealing)
        {
            LastTimeHealed = Time.time;

            if (Health.Value + HealAmount > MaxHealth)
            {
                Health.Value = MaxHealth;

                return;
            }

            Health.Value += HealAmount;
        }
    }

    public void SendToGraveyard()
    {
        if (Health.Value <= 0)
        {
            SelfTransform.position = GameManager.Singleton.GetGraveyardLocation();
        }
    }

    public void EnterFirstPerson()
    {
        CharacterModel.transform.localPosition = CharacterModelOriginalPosition;
        CharacterModel.transform.localRotation = CharacterModelOriginalRotation;
        ThirdPersonComponents.SetActive(false);

        Weapons.EnableFist();
    }

    public void EnterThirdPerson()
    {
        CharacterModel.transform.localPosition = CharacterModelOriginalPosition;
        CharacterModel.transform.localRotation = CharacterModelOriginalRotation;
        ThirdPersonComponents.SetActive(true);

        Weapons.DisableFist();
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
            OriginalPosition = SelfTransform.position;
            SelfTransform.position = RewindedPosition;

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

        SelfTransform.position = OriginalPosition;
    }

    public int GetPingInTick()
    {
        return (int)(NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(OwnerClientId) * 0.05f);
    }

    public int GetHalfRTTInTick()
    {
        return GetPingInTick() + ServerDelay;
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

    public Vector3 GetHandPosition()
    {
        return HandTransform.position;
    }

    public Vector3 GetAimPointLocation()
    {
        return ThrowBallLocationTransform.position;
    }

    public void EnterDunk()
    {
        Weapons.EnterDunk();
    }

    public void ExitDunk()
    {
        Weapons.ExitDunk();
    }

    public bool GetIsDunking()
    {
        return Weapons.bHoldingBall && Movement.GetIsGroundPounding();
    }

    public void Attach()
    {
        Weapons.Attach();
    }

    public void Unattach()
    {
        Weapons.Detach();
    }

    public Vector3 GetVelocity()
    {
        return Movement.GetVelocity();
    }

    public bool GetIsHoldingBall()
    {
        return Weapons.bHoldingBall;
    }

    public void OnScore(Vector3 NewVelocity)
    {
        if (Weapons.bHoldingBall)
        {
            Ball.Singleton.Detach();
        }

        Movement.OnScore(NewVelocity);
    }

    public void TeleportTo(Vector3 pos)
    {
        SelfTransform.position = pos;
    }

    private void SetGameLayerRecursive(GameObject gameObject, int layer)
    {
        var children = gameObject.GetComponentsInChildren<Transform>(includeInactive: true);

        foreach (var child in children)
        {
            child.gameObject.layer = layer;
        }
    }

    private void TryInitAgain()
    {
        List<PlayerInformation> PlayerList = GameManager.Singleton.GetAllPlayerInformation();

        bool bfound = false;

        foreach (PlayerInformation playerInfo in PlayerList)
        {
            if (playerInfo.Id == OwnerClientId)
            {
                Team = playerInfo.Team;

                bfound = true;

                break;
            }
        }

        if (!bfound)
        {
            Invoke(nameof(TryInitAgain), 1);

            return;
        }

        if (IsOwner)
        {
            if (Team == Teams.Red)
            {
                FirstPersonHealthBar.GetFillImage().color = Color.red;
            }

            else if (Team == Teams.Blue)
            {
                FirstPersonHealthBar.GetFillImage().color = Color.blue;
            }

            return;
        }

        if (Team == Teams.Red)
        {
            ThirdPersonHealthBar.GetFillImage().color = Color.red;

            return;
        }

        if (Team == Teams.Blue)
        {
            ThirdPersonHealthBar.GetFillImage().color = Color.blue;

            return;
        }
    }

    public virtual bool GetCanCarryBall()
    {
        return true;
    }
}
