using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseWeapon : MonoBehaviour
{
    [SerializeField]
    public WeaponManager Manager;

    [SerializeField]
    public GameObject WeaponModel;
    [SerializeField]
    public PlayerMovement PlayerMovementComponent;

    public List<PlayerManager> RewindedPlayerList;
    public LayerMask PlayerLayer;

    public int AmmoCount;

    public int FireCooldown1;
    public int FireCooldown2;

    public float Damage;
    public float Damage2;

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
