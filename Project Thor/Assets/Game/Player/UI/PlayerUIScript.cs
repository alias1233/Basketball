using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerUIScript : MonoBehaviour
{
    public TMP_Text DashAbilityBar;
    public TMP_Text FistChargeBar;

    public PlayerManager Player;
    public PlayerMovement playermovement;
    public WeaponManager weaponmanager;

    public int UpdateUIInterval;
    private int tick;

    private bool bIsChangingDash;

    private bool bIsChangingFist;

    private void FixedUpdate()
    {
        tick++;

        if(tick < UpdateUIInterval)
        {
            return;
        }

        tick = 0;

        int CurrentTimeStamp = Player.GetTimeStamp();

        float TimeBetweenDash = CurrentTimeStamp - playermovement.GetLastTimeDash();
        int DashCooldown = playermovement.DashCooldown;

        if (TimeBetweenDash <= DashCooldown)
        {
            DashAbilityBar.text = (TimeBetweenDash / DashCooldown).ToString();

            bIsChangingDash = true;
        }

        else if(bIsChangingDash)
        {
            DashAbilityBar.text = "1";

            bIsChangingDash = false;
        }

        if(weaponmanager.GetIsChargingFist())
        {
            float maxchargingtime = weaponmanager.MaxChargingTime;
            float chargingtime = (CurrentTimeStamp - weaponmanager.GetFistStartChargeTime());

            if(chargingtime > maxchargingtime)
            {
                FistChargeBar.text = "1";
            }

            else
            {
                FistChargeBar.text = (chargingtime / maxchargingtime).ToString();
            }
        }
    }
}
