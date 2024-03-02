using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static ConnectionNotificationManager;

public enum ActiveWeaponNumber
{
    Laser,
    Pistol,
    Sword
}

public class WeaponManager : NetworkBehaviour
{
    [Header("Components")]

    private PlayerManager Player;
    public Transform TPOrientation;
    public Transform FPOrientation;

    public Transform AimPoint;

    [SerializeField]
    private List<BaseWeapon> WeaponList;
    public ObjectPool BulletPool;

    [Header("Client Data")]

    private int CurrentTimeStamp;

    private NetworkRole LocalRole;
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

    private void Start()
    {
        Player = GetComponent<PlayerManager>();

        LocalRole = Player.GetLocalRole();

        ActiveWeaponIndex = ActiveWeaponNumber.Laser;
        ActiveWeapon = WeaponList[0];
        ActiveWeapon.ChangeActive(true);

        if (IsServer)
        {
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

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ConnectionNotificationManager.Singleton.OnClientConnectionNotification += UpdateClientSendRPCParams;
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
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.Laser);
        }

        else if (Input.GetKey(KeyCode.Alpha2))
        {
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.Pistol);
        }

        else if (Input.GetKey(KeyCode.Alpha3))
        {
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.Sword);
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
            CurrentInput = input;
            OnChangeActiveWeapon(ActiveWeaponIndex, CurrentInput.ActiveWeapon);

            InputsDictionary.Remove(CurrentTimeStamp);

            bReplicateInput = true;
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
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.Laser);
        }

        else if (Input.GetKey(KeyCode.Alpha2))
        {
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.Pistol);
        }

        else if (Input.GetKey(KeyCode.Alpha3))
        {
            OnChangeActiveWeapon(ActiveWeaponIndex, ActiveWeaponNumber.Sword);
        }

        if(!bReplicateInput)
        {
            bReplicateInput =
                !(
                CurrentInput.Mouse1 == Input.GetKey(KeyCode.Mouse0) &&
                CurrentInput.Mouse2 == Input.GetKey(KeyCode.Mouse1)
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

            SendWeaponInputsServerRpc(CurrentInput);
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
        if(bReplicateInput)
        {
            if (Time.time - LastTimeReplicatedWeaponSwitch >= ReplicateWeaponSwitchCooldown)
            {
                LastTimeReplicatedWeaponSwitch = Time.time;

                bReplicateInput = false;

                ReplicateWeaponSwitchClientRpc(ActiveWeaponIndex, IgnoreOwnerRPCParams);
            }
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
            case ActiveWeaponNumber.Laser:

                ActiveWeapon = WeaponList[0];

                break;

            case ActiveWeaponNumber.Pistol:

                ActiveWeapon = WeaponList[1];

                break;

            case ActiveWeaponNumber.Sword:

                ActiveWeapon = WeaponList[2];

                break;
        }

        ActiveWeapon.ChangeActive(true);
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
            ReplicateFire1ClientRpc(ActiveWeaponIndex, FPOrientation.transform.rotation, IgnoreOwnerRPCParams);

            return;
        }
        
        ReplicateFire2ClientRpc(ActiveWeaponIndex, FPOrientation.transform.rotation, IgnoreOwnerRPCParams);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateFire1ClientRpc(ActiveWeaponNumber activeweapon, Quaternion rotation, ClientRpcParams clientRpcParams = default)
    {
        FPOrientation.rotation = rotation;
        float a = Mathf.Sqrt((rotation.w * rotation.w) + (rotation.y * rotation.y));
        TPOrientation.rotation = new Quaternion(0, rotation.y / a, 0, rotation.w / a);

        OnChangeActiveWeapon(ActiveWeaponIndex, activeweapon);

        ActiveWeapon.Visuals1();
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateFire2ClientRpc(ActiveWeaponNumber activeweapon, Quaternion rotation, ClientRpcParams clientRpcParams = default)
    {
        FPOrientation.rotation = rotation;
        float a = Mathf.Sqrt((rotation.w * rotation.w) + (rotation.y * rotation.y));
        TPOrientation.rotation = new Quaternion(0, rotation.y / a, 0, rotation.w / a);

        OnChangeActiveWeapon(ActiveWeaponIndex, activeweapon);

        ActiveWeapon.Visuals2();
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateWeaponSwitchClientRpc(ActiveWeaponNumber activeweapon, ClientRpcParams clientRpcParams = default)
    {
        OnChangeActiveWeapon(ActiveWeaponIndex, activeweapon);
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
