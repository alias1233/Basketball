using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
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

    public float ReplicateShootingCooldown = 0.15f;
    private float LastTimReplicatedShooting;

    public bool IsShooting1;
    public bool IsShooting2;

    [Header("Client Data")]

    public int Radius;

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

                SimulatedProxyTick();

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
        if (Time.time - LastTimReplicatedShooting >= ReplicateShootingCooldown)
        {
            LastTimReplicatedShooting = Time.time;

            ReplicateShootingClientRpc(ActiveWeaponIndex, CurrentInput.Mouse1, CurrentInput.Mouse2, IgnoreOwnerRPCParams);
        }
    }

    void SimulatedProxyTick()
    {
        if (IsShooting1)
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

        if (IsShooting2)
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

    [ServerRpc]
    public void HitPlayerServerRPC(NetworkObjectReference playerGameObject)
    {
        
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateShootingClientRpc(ActiveWeaponNumber activeweapon, bool mouse1, bool mouse2, ClientRpcParams clientRpcParams = default)
    {
        OnChangeActiveWeapon(ActiveWeaponIndex, activeweapon);
        IsShooting1 = mouse1;
        IsShooting2 = mouse2;
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
        return Radius;
    }

    public int GetTimeStamp()
    {
        return Player.GetTimeStamp();
    }
}
