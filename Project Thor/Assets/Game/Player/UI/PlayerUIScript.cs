using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerUIScript : MonoBehaviour
{
    public TMP_Text DashAbilityBar;

    public PlayerManager Player;
    public PlayerMovement playermovement;

    public int UpdateUIInterval;
    private int tick;

    private bool bIsChanging;

    private void FixedUpdate()
    {
        tick++;

        if(tick < UpdateUIInterval)
        {
            return;
        }

        tick = 0;

        float TimeBetweenDash = Player.GetTimeStamp() - playermovement.GetLastTimeDash();
        int DashCooldown = playermovement.DashCooldown;

        if (TimeBetweenDash <= DashCooldown)
        {
            DashAbilityBar.text = (TimeBetweenDash / DashCooldown).ToString();

            bIsChanging = true;

            return;
        }

        if(bIsChanging)
        {
            DashAbilityBar.text = "1";

            bIsChanging = false;
        }
    }
}
