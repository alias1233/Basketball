using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseWeapon : MonoBehaviour
{
    public GameObject WeaponModel;
    public PlayerMovement PlayerMovementComponent;

    public int AmmoCount;

    public int FireCooldown1;
    public int FireCooldown2;

    public int Damage;
    public int Damage2;

    public virtual void ChangeActive(bool active)
    {
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
