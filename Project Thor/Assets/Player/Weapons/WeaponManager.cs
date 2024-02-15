using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField]
    private List<BaseWeapon> WeaponList;

    private BaseWeapon ActiveWeapon;

    private void Start()
    {
        ActiveWeapon = WeaponList[0];

        ActiveWeapon.ChangeActive(true);
    }

    private void FixedUpdate()
    {
        
    }
}
