using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerUIScript : MonoBehaviour
{
    public ProgressBar DashAbilityBar;
    public ProgressBar GrappleAbilityBar;

    public PlayerManager Player;
    public PlayerMovement playermovement;

    public int UpdateUIInterval;
    private int tick;

    private bool bIsChangingDash;

    private bool bIsChangingGrapple;

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
            DashAbilityBar.UpdateProgressBar(TimeBetweenDash / DashCooldown);

            bIsChangingDash = true;
        }

        else if(bIsChangingDash)
        {
            DashAbilityBar.UpdateProgressBar(1);

            bIsChangingDash = false;
        }

        float TimeBetweenGrapple = CurrentTimeStamp - playermovement.GetLastTimeGrapple();
        int GrappleCooldown = playermovement.GrappleShootCooldown;

        if (TimeBetweenGrapple <= GrappleCooldown)
        {
            GrappleAbilityBar.UpdateProgressBar(TimeBetweenGrapple / GrappleCooldown);

            bIsChangingGrapple = true;
        }

        else if (bIsChangingGrapple)
        {
            GrappleAbilityBar.UpdateProgressBar(1);

            bIsChangingGrapple = false;
        }
    }
}
