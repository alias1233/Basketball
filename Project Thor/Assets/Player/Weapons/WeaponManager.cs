using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class WeaponManager : NetworkBehaviour
{
    [SerializeField]
    private List<BaseWeapon> WeaponList;

    public enum ActiveWeaponNumber
    {
        Laser,
        Sword
    }

    public NetworkVariable<ActiveWeaponNumber> ActiveWeaponIndex = new NetworkVariable<ActiveWeaponNumber>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private BaseWeapon ActiveWeapon;

    private int TimeStamp;

    public bool IsShooting1;
    public bool IsShooting2;

    private int LastTimeShot1;
    private int LastTimeShot2;

    public override void OnNetworkSpawn()
    {
        ActiveWeaponIndex.Value = ActiveWeaponNumber.Laser;
        ActiveWeaponIndex.OnValueChanged += OnChangeActiveWeapon;

        ActiveWeapon = WeaponList[0];
        ActiveWeapon.ChangeActive(true);
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
        TimeStamp++;

        if (!IsOwner)
        {
            if(IsShooting1)
            {
                if (TimeStamp - LastTimeShot1 >= ActiveWeapon.FireCooldown1)
                {
                    LastTimeShot1 = TimeStamp;

                    ActiveWeapon.Fire1();
                }
            }

            else
            {
                ActiveWeapon.StopFire1();
            }

            if (IsShooting2)
            {
                if (TimeStamp - LastTimeShot2 >= ActiveWeapon.FireCooldown2)
                {
                    LastTimeShot2 = TimeStamp;

                    ActiveWeapon.Fire2();
                }
            }

            else
            {
                ActiveWeapon.StopFire2();
            }

            return;
        }

        if(Input.GetKey(KeyCode.Mouse0))
        {
            if(TimeStamp - LastTimeShot1 >= ActiveWeapon.FireCooldown1)
            {
                LastTimeShot1 = TimeStamp;

                ActiveWeapon.Fire1();
            }
        }

        else
        {
            ActiveWeapon.StopFire1();
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            if (TimeStamp - LastTimeShot2 >= ActiveWeapon.FireCooldown2)
            {
                LastTimeShot2 = TimeStamp;

                ActiveWeapon.Fire2();
            }
        }

        else
        {
            ActiveWeapon.StopFire2();
        }
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void StartFiring1ServerRpc()
    {
        IsShooting1 = true;
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void StopFiring1ServerRpc()
    {
        IsShooting1 = false;
    }

    public bool GetHasAuthority()
    {
        return IsServer;
    }
}
