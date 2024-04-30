using UnityEngine;

public class V1UIScript : BasePlayerUI
{
    private V1Movement V1movement;

    public ProgressBar DashAbilityBar;
    public ProgressBar GrappleAbilityBar;
    public ProgressBar FlyAbilityBar;

    private bool bIsChangingDash;
    private bool bIsChangingGrapple;
    private bool bChangingFly;

    private void Awake()
    {
        V1movement = (V1Movement)playermovement;
    }

    protected override void UpdateUI(int CurrentTimeStamp)
    {
        base.UpdateUI(CurrentTimeStamp);

        float TimeBetweenDash = CurrentTimeStamp - V1movement.GetLastTimeDash();
        int DashCooldown = V1movement.DashCooldown;

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

        float TimeBetweenGrapple = CurrentTimeStamp - V1movement.GetLastTimeGrapple();
        int GrappleCooldown = V1movement.GrappleShootCooldown;

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

        float TimeBetweenFly = CurrentTimeStamp - V1movement.GetLastTimeFly();
        int FlightCooldown = V1movement.FlyCooldown;

        if(TimeBetweenFly <= FlightCooldown)
        {
            FlyAbilityBar.UpdateProgressBar(TimeBetweenFly / FlightCooldown);

            bChangingFly = true;
        }

        else if(bChangingFly)
        {
            FlyAbilityBar.UpdateProgressBar(1);

            bChangingFly = false;
        }
    }
}
