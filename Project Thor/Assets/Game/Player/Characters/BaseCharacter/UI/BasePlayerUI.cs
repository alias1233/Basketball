using UnityEngine;

public class BasePlayerUI : MonoBehaviour
{
    public BasePlayerManager Player;
    public BaseCharacterMovement playermovement;

    public int UpdateUIInterval;
    private int tick;

    private void FixedUpdate()
    {
        tick++;

        if (tick < UpdateUIInterval)
        {
            return;
        }

        tick = 0;

        UpdateUI(Player.GetTimeStamp());
    }

    protected virtual void UpdateUI(int CurrentTimeStamp)
    {

    }
}
