using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class WeaponManager : NetworkBehaviour
{
    public enum ActiveWeaponNumber
    {
        Laser,
        Sword
    }

    [Header("Components")]

    [SerializeField]
    private PlayerManager Player;

    private int CurrentTimeStamp;

    private NetworkRole LocalRole;

    [SerializeField]
    private List<BaseWeapon> WeaponList;

    [Header("Client Data")]

    public float SendInputCooldown = 0.1f;
    private float LastTimeSentInputs;

    private Dictionary<int, WeaponInputs> InputsDictionary = new Dictionary<int, WeaponInputs>();
    private WeaponInputs CurrentInput;

    private NetworkVariable<ActiveWeaponNumber> ActiveWeaponIndex = new NetworkVariable<ActiveWeaponNumber>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private BaseWeapon ActiveWeapon;

    public float ReplicateShootingCooldown = 0.15f;
    private float LastTimReplicatedShooting;

    public bool IsShooting1;
    public bool IsShooting2;

    private int LastTimeShot1;
    private int LastTimeShot2;

    [Header("Client Data")]

    public int Radius;

    public TMP_Text PINGTEXT;

    public override void OnNetworkSpawn()
    {
        ActiveWeaponIndex.Value = ActiveWeaponNumber.Laser;
        ActiveWeaponIndex.OnValueChanged += OnChangeActiveWeapon;

        ActiveWeapon = WeaponList[0];
        ActiveWeapon.ChangeActive(true);

        LocalRole = Player.GetLocalRole();
    }

    void OnChangeActiveWeapon(ActiveWeaponNumber previous, ActiveWeaponNumber current)
    {
        ActiveWeapon.ChangeActive(false);

        switch (current)
        {
            case ActiveWeaponNumber.Laser:

                ActiveWeapon = WeaponList[0];

                break;

            case ActiveWeaponNumber.Sword:

                ActiveWeapon = WeaponList[1];

                break;
        }

        ActiveWeapon.ChangeActive(true);
    }

    private void FixedUpdate()
    {
        if (Player.GetIsDead())
        {
            return;
        }

        CurrentTimeStamp = Player.GetTimeStamp();

        if (IsOwner)
        {
            OwnerTick();

            if (!IsServer)
            {
                return;
            }

            if (Time.time - LastTimReplicatedShooting >= ReplicateShootingCooldown)
            {
                LastTimReplicatedShooting = Time.time;

                ReplicateShootingClientRpc(CurrentInput.Mouse1, CurrentInput.Mouse2);
            }

            return;
        }

        if(IsServer)
        {
            TickForOThers();

            return;
        }

        SimulatedProxyTick();
    }

    private void OwnerTick()
    {
        if(Time.time - LastTimeSentInputs >= SendInputCooldown)
        {
            LastTimeSentInputs = Time.time;

            CurrentInput = new WeaponInputs(CurrentTimeStamp, Input.GetKey(KeyCode.Mouse0), Input.GetKey(KeyCode.Mouse1));

            SendWeaponInputsServerRpc(CurrentInput);
        }

        if (CurrentInput.Mouse1)
        {
            if (CurrentTimeStamp - LastTimeShot1 >= ActiveWeapon.FireCooldown1)
            {
                LastTimeShot1 = CurrentTimeStamp;

                ActiveWeapon.Fire1();
            }
        }

        else
        {
            ActiveWeapon.StopFire1();
        }

        if (CurrentInput.Mouse2)
        {
            if (CurrentTimeStamp - LastTimeShot2 >= ActiveWeapon.FireCooldown2)
            {
                LastTimeShot2 = CurrentTimeStamp;

                ActiveWeapon.Fire2();
            }
        }

        else
        {
            ActiveWeapon.StopFire2();
        }
    }

    private void TickForOThers()
    {
        if(InputsDictionary.TryGetValue(CurrentTimeStamp, out var input))
        {
            CurrentInput = input;
        }

        if (CurrentInput.Mouse1)
        {
            if (CurrentTimeStamp - LastTimeShot1 >= ActiveWeapon.FireCooldown1)
            {
                LastTimeShot1 = CurrentTimeStamp;

                ActiveWeapon.Fire1();
            }
        }

        else
        {
            ActiveWeapon.StopFire1();
        }

        if (CurrentInput.Mouse1)
        {
            if (CurrentTimeStamp - LastTimeShot2 >= ActiveWeapon.FireCooldown2)
            {
                LastTimeShot2 = CurrentTimeStamp;

                ActiveWeapon.Fire2();
            }
        }

        else
        {
            ActiveWeapon.StopFire2();
        }
    }

    private void SimulatedProxyTick()
    {
        if (IsShooting1)
        {
            if (CurrentTimeStamp - LastTimeShot1 >= ActiveWeapon.FireCooldown1)
            {
                LastTimeShot1 = CurrentTimeStamp;

                ActiveWeapon.Fire1();
            }
        }

        else
        {
            ActiveWeapon.StopFire1();
        }

        if (IsShooting2)
        {
            if (CurrentTimeStamp - LastTimeShot2 >= ActiveWeapon.FireCooldown2)
            {
                LastTimeShot2 = CurrentTimeStamp;

                ActiveWeapon.Fire2();
            }
        }

        else
        {
            ActiveWeapon.StopFire2();
        }
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void SendWeaponInputsServerRpc(WeaponInputs input)
    {
        InputsDictionary.Add(input.TimeStamp, input);

        Player.CheckClientTimeError(input.TimeStamp);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateShootingClientRpc(bool mouse1, bool mouse2)
    {
        if (IsOwner || IsServer)
        {
            return;
        }

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
        PINGTEXT.text = Player.GetPingInTick().ToString();

        return Player.GetPingInTick();
    }

    public int GetRadius()
    {
        return Radius;
    }
}
