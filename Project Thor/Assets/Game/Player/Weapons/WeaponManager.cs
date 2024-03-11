using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static ConnectionNotificationManager;

public enum ActiveWeaponNumber
{
    Shotgun,
    Pistol,
    RocketLauncher
}

public class WeaponManager : NetworkBehaviour
{
    [Header("Components")]

    private PlayerManager Player;
    private PlayerMovement PlayerMovementComponent;
    public Transform TPOrientation;
    public Transform FPOrientation;

    public Transform AimPoint;

    [SerializeField]
    private List<BaseWeapon> WeaponList;

    public ObjectPool BulletPool;
    public ObjectPool RocketPool;

    public ObjectPool ProjectilePool;

    public AudioSource HitSound;

    [Header("Client Data")]

    private int CurrentTimeStamp;

    private NetworkRole LocalRole;
    private ClientRpcParams OwningClientID;
    private ClientRpcParams IgnoreOwnerRPCParams;
    private List<ulong> ClientIDList = new List<ulong>();

    public float SendInputCooldown = 0.1f;
    private float LastTimeSentInputs;
    private bool bReplicateInput;

    private Dictionary<int, WeaponInputs> InputsDictionary = new Dictionary<int, WeaponInputs>();
    private WeaponInputs CurrentInput;

    private ActiveWeaponNumber ActiveWeaponIndex;
    private BaseWeapon ActiveWeapon;

    public bool IsShooting1;
    public bool IsShooting2;

    public int RadiusOfRewindCheck;

    [Header("Replicate Firing")]

    public float ReplicateWeaponSwitchCooldown = 0.5f;
    private float LastTimeReplicatedWeaponSwitch;

    [Header("Melee")]

    public GameObject Fist;

    public LayerMask PlayerLayer;

    [SerializeField]
    private MeleeAnimScript meleeanimation;

    public AudioSource PunchSwooshSound;
    public AudioSource PunchHitSound;

    public int MeleeCooldown;
    public int MeleeRange;
    public int MeleeDamage;

    private int LastTimeMelee;

    public float Radius;

    private RaycastHit[] Hits = new RaycastHit[5];

    private void Start()
    {
        Player = GetComponent<PlayerManager>();
        PlayerMovementComponent = GetComponent<PlayerMovement>();

        LocalRole = Player.GetLocalRole();

        ActiveWeaponIndex = ActiveWeaponNumber.Shotgun;
        ActiveWeapon = WeaponList[0];
        ActiveWeapon.ChangeActive(true);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ConnectionNotificationManager.Singleton.OnClientConnectionNotification += UpdateClientSendRPCParams;

            OwningClientID = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            };

            foreach (ulong i in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (i != OwnerClientId && i != 0)
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
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            ConnectionNotificationManager.Singleton.OnClientConnectionNotification -= UpdateClientSendRPCParams;
        }
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

        IgnoreOwnerRPCParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = ClientIDList
            }
        };
    }

    public void FixedTick(int timestamp)
    {
        CurrentTimeStamp = timestamp;

        switch (LocalRole)
        {
            case NetworkRole.HostOwner:

                HostTick();
                ServerTickForAll();

                break;

            case NetworkRole.HostProxy:

                ServerTickForOtherPlayers();
                ServerTickForAll();

                break;

            case NetworkRole.AutonomousProxy:

                AutonomousProxyTick();

                break;

            case NetworkRole.SimulatedProxy:



                break;
        }
    }

    void HostTick()
    {
        if (Input.GetKey(KeyCode.Alpha1))
        {
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.Shotgun);
        }

        else if (Input.GetKey(KeyCode.Alpha2))
        {
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.Pistol);
        }

        else if (Input.GetKey(KeyCode.Alpha3))
        {
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.RocketLauncher);
        }

        if (Input.GetKey(KeyCode.F))
        {
            if (CurrentTimeStamp - LastTimeMelee >= MeleeCooldown)
            {
                LastTimeMelee = CurrentTimeStamp;

                Melee();
            }
        }

        if (Input.GetKey(KeyCode.Mouse0))
        {
            if (CurrentTimeStamp - ActiveWeapon.LastTimeShot1 >= ActiveWeapon.FireCooldown1)
            {
                ActiveWeapon.LastTimeShot1 = CurrentTimeStamp;

                ActiveWeapon.Fire1();
            }
        }

        else
        {
            ActiveWeapon.StopFire1();
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            if (CurrentTimeStamp - ActiveWeapon.LastTimeShot2 >= ActiveWeapon.FireCooldown2)
            {
                ActiveWeapon.LastTimeShot2 = CurrentTimeStamp;

                ActiveWeapon.Fire2();
            }
        }

        else
        {
            ActiveWeapon.StopFire2();
        }
    }

    void ServerTickForOtherPlayers()
    {
        if (InputsDictionary.TryGetValue(CurrentTimeStamp, out var input))
        {
            InputsDictionary.Remove(CurrentTimeStamp);
            CurrentInput = input;

            OnChangeActiveWeapon(ActiveWeaponIndex, CurrentInput.ActiveWeapon);
            bReplicateInput = true;
        }

        if (CurrentInput.F)
        {
            if (CurrentTimeStamp - LastTimeMelee >= MeleeCooldown)
            {
                LastTimeMelee = CurrentTimeStamp;

                Melee();
            }
        }

        if (CurrentInput.Mouse1)
        {
            if (CurrentTimeStamp - ActiveWeapon.LastTimeShot1 >= ActiveWeapon.FireCooldown1)
            {
                ActiveWeapon.LastTimeShot1 = CurrentTimeStamp;

                ActiveWeapon.Fire1();
            }
        }

        else
        {
            ActiveWeapon.StopFire1();
        }

        if (CurrentInput.Mouse2)
        {
            if (CurrentTimeStamp - ActiveWeapon.LastTimeShot2 >= ActiveWeapon.FireCooldown2)
            {
                ActiveWeapon.LastTimeShot2 = CurrentTimeStamp;

                ActiveWeapon.Fire2();
            }
        }

        else
        {
            ActiveWeapon.StopFire2();
        }
    }

    void AutonomousProxyTick()
    {
        if (Input.GetKey(KeyCode.Alpha1))
        {
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.Shotgun);
        }

        else if (Input.GetKey(KeyCode.Alpha2))
        {
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.Pistol);
        }

        else if (Input.GetKey(KeyCode.Alpha3))
        {
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.RocketLauncher);
        }

        if(!bReplicateInput)
        {
            bReplicateInput =
                !(
                CurrentInput.Mouse1 == Input.GetKey(KeyCode.Mouse0) &&
                CurrentInput.Mouse2 == Input.GetKey(KeyCode.Mouse1) && 
                CurrentInput.F == Input.GetKey(KeyCode.F)
                );
        }

        if (Time.time - LastTimeSentInputs >= SendInputCooldown || bReplicateInput)
        {
            bReplicateInput = false;

            LastTimeSentInputs = Time.time;

            CurrentInput.TimeStamp = CurrentTimeStamp;
            CurrentInput.ActiveWeapon = ActiveWeaponIndex;
            CurrentInput.Mouse1 = Input.GetKey(KeyCode.Mouse0);
            CurrentInput.Mouse2 = Input.GetKey(KeyCode.Mouse1);
            CurrentInput.F = Input.GetKey(KeyCode.F);

            SendWeaponInputsServerRpc(CurrentInput);
        }

        if (CurrentInput.F)
        {
            if (CurrentTimeStamp - LastTimeMelee >= MeleeCooldown)
            {
                LastTimeMelee = CurrentTimeStamp;

                Melee();
            }
        }

        if (CurrentInput.Mouse1)
        {
            if (CurrentTimeStamp - ActiveWeapon.LastTimeShot1 >= ActiveWeapon.FireCooldown1)
            {
                ActiveWeapon.LastTimeShot1 = CurrentTimeStamp;

                ActiveWeapon.Fire1();
            }
        }

        else
        {
            ActiveWeapon.StopFire1();
        }

        if (CurrentInput.Mouse2)
        {
            if (CurrentTimeStamp - ActiveWeapon.LastTimeShot2 >= ActiveWeapon.FireCooldown2)
            {
                ActiveWeapon.LastTimeShot2 = CurrentTimeStamp;

                ActiveWeapon.Fire2();
            }
        }

        else
        {
            ActiveWeapon.StopFire2();
        }
    }

    void ServerTickForAll()
    {
        if(!bReplicateInput)
        {
            return;
        }

        if (Time.time - LastTimeReplicatedWeaponSwitch >= ReplicateWeaponSwitchCooldown)
        {
            LastTimeReplicatedWeaponSwitch = Time.time;

            bReplicateInput = false;

            ReplicateWeaponSwitchClientRpc(ActiveWeaponIndex, IgnoreOwnerRPCParams);
        }
    }

    void OnChangeActiveWeapon(ActiveWeaponNumber previous, ActiveWeaponNumber newweapon)
    {
        if (previous == newweapon)
        {
            return;
        }

        bReplicateInput = true;

        ActiveWeapon.ChangeActive(false);
        ActiveWeaponIndex = newweapon;

        switch (ActiveWeaponIndex)
        {
            case ActiveWeaponNumber.Shotgun:

                ActiveWeapon = WeaponList[0];

                break;

            case ActiveWeaponNumber.Pistol:

                ActiveWeapon = WeaponList[1];

                break;

            case ActiveWeaponNumber.RocketLauncher:

                ActiveWeapon = WeaponList[2];

                break;
        }

        ActiveWeapon.ChangeActive(true);
    }

    private void Melee()
    {
        MeleeVisual();

        if (!IsServer)
        {
            int NumHits2 = Physics.SphereCastNonAlloc(new Ray(GetAimPointLocation(), PlayerMovementComponent.GetRotation() * Vector3.forward), Radius, Hits, MeleeRange, PlayerLayer);
            bool bHit2 = false;

            for (int i = 0; i < NumHits2; i++)
            {
                if (Hits[i].transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager stats))
                {
                    if (!stats.IsSameTeam(GetTeam()))
                    {
                        bHit2 = true;
                    }
                }
            }

            if (bHit2)
            {
                PunchHitSound.Play();
            }

            return;
        }

        ReplicateFire(3);

        Ray CenterRay = new Ray(GetAimPointLocation(), PlayerMovementComponent.GetRotation() * Vector3.forward);

        if (!IsOwner)
        {
            if (!ActiveWeapon.RewindPlayers(CenterRay, MeleeRange))
            {
                return;
            }
        }

        int NumHits = Physics.SphereCastNonAlloc(CenterRay, Radius, Hits, MeleeRange, PlayerLayer);
        bool bHit = false;

        for (int i = 0; i < NumHits; i++)
        {
            if (Hits[i].transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager stats))
            {
                if (stats.Damage(GetTeam(), MeleeDamage))
                {
                    bHit = true;
                }
            }
        }

        if(bHit)
        {
            PunchHitSound.Play();
        }

        ActiveWeapon.ResetRewindedPlayers();
    }

    private void MeleeVisual()
    {
        Fist.SetActive(true);
        meleeanimation.PunchAnim();

        PunchSwooshSound.Play();
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void SendWeaponInputsServerRpc(WeaponInputs input)
    {
        if(input.TimeStamp > CurrentTimeStamp)
        {
            InputsDictionary[input.TimeStamp] = input;
        }

        Player.CheckClientTimeError(input.TimeStamp);
    }

    public void ReplicateFire(int FireNum)
    {
        LastTimeReplicatedWeaponSwitch = Time.time;

        if (FireNum == 1)
        {
            ReplicateFire1ClientRpc(ActiveWeaponIndex, PlayerMovementComponent.GetRotation(), IgnoreOwnerRPCParams);

            return;
        }

        if(FireNum == 2)
        {
            ReplicateFire2ClientRpc(ActiveWeaponIndex, PlayerMovementComponent.GetRotation(), IgnoreOwnerRPCParams);

            return;
        }

        ReplicateMeleeClientRpc(ActiveWeaponIndex, IgnoreOwnerRPCParams);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateFire1ClientRpc(ActiveWeaponNumber activeweapon, Quaternion rotation, ClientRpcParams clientRpcParams = default)
    {
        PlayerMovementComponent.SetRotation(rotation);

        FPOrientation.rotation = rotation;
        float a = Mathf.Sqrt((rotation.w * rotation.w) + (rotation.y * rotation.y));
        TPOrientation.rotation = new Quaternion(0, rotation.y / a, 0, rotation.w / a);

        OnChangeActiveWeapon(ActiveWeaponIndex, activeweapon);

        ActiveWeapon.Visuals1();
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateFire2ClientRpc(ActiveWeaponNumber activeweapon, Quaternion rotation, ClientRpcParams clientRpcParams = default)
    {
        PlayerMovementComponent.SetRotation(rotation);

        FPOrientation.rotation = rotation;
        float a = Mathf.Sqrt((rotation.w * rotation.w) + (rotation.y * rotation.y));
        TPOrientation.rotation = new Quaternion(0, rotation.y / a, 0, rotation.w / a);

        OnChangeActiveWeapon(ActiveWeaponIndex, activeweapon);

        ActiveWeapon.Visuals2();
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateMeleeClientRpc(ActiveWeaponNumber activeweapon, ClientRpcParams clientRpcParams = default)
    {
        OnChangeActiveWeapon(ActiveWeaponIndex, activeweapon);

        MeleeVisual();
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateWeaponSwitchClientRpc(ActiveWeaponNumber activeweapon, ClientRpcParams clientRpcParams = default)
    {
        OnChangeActiveWeapon(ActiveWeaponIndex, activeweapon);
    }

    public void PlayHitSound()
    {
        if(IsOwner)
        {
            HitSound.Play();

            return;
        }

        ReplicateHitClientRpc(OwningClientID);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateHitClientRpc(ClientRpcParams clientRpcParams = default)
    {
        HitSound.Play();
    }

    public Vector3 GetAimPointLocation()
    {
        return AimPoint.position;
    }

    public bool GetIsOwner()
    {
        return IsOwner;
    }

    public bool GetHasAuthority()
    {
        return IsServer;
    }

    public Teams GetTeam()
    {
        return Player.GetTeam();
    }

    public int GetPingInTick()
    {
        return Player.GetPingInTick();
    }

    public int GetRadius()
    {
        return RadiusOfRewindCheck;
    }

    public int GetTimeStamp()
    {
        return Player.GetTimeStamp();
    }
}
