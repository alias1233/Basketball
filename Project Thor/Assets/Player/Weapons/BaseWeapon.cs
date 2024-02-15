using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class BaseWeapon : NetworkBehaviour
{
    public GameObject WeaponModel;
    public PlayerMovement PlayerMovementComponent;

    private bool bIsActive;
    private int TimeStamp;

    public int AmmoCount;

    public int FireCooldown1;
    public int FireCooldown2;

    private bool IsShooting1;
    private bool IsShooting2;

    public int Damage;
    public int Damage2;

    private int LastTimeShot1;
    private int LastTimeShot2;

    private void FixedUpdate()
    {
        if(!bIsActive)
        {
            return;
        }

        TimeStamp++;

        if (!IsOwner)
        {
            return;
        }

        if(Input.GetKey(KeyCode.Mouse0) || IsShooting1)
        {
            if(TimeStamp - LastTimeShot1 >= FireCooldown1)
            {
                LastTimeShot1 = TimeStamp;

                Fire1();
            }
        }

        else
        {
            StopFire1();
        }

        if (Input.GetKey(KeyCode.Mouse1) || IsShooting2)
        {
            if (TimeStamp - LastTimeShot2 >= FireCooldown2)
            {
                LastTimeShot2 = TimeStamp;

                Fire2();
            }
        }

        else
        {
            StopFire2();
        }
    }

    public virtual void ChangeActive(bool active)
    {
        bIsActive = active;

        if(active)
        {
            WeaponModel.SetActive(true);

            return;
        }

        WeaponModel.SetActive(false);
    }

    public virtual void Fire1()
    {

    }

    public virtual void Fire2()
    {

    }

    public virtual void StopFire1()
    {

    }

    public virtual void StopFire2()
    {

    }
}
