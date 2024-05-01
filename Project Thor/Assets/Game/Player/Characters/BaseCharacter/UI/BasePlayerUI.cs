using UnityEngine;
using UnityEngine.Rendering.Universal;

public class BasePlayerUI : MonoBehaviour
{
    public BaseCharacterComponents Components;

    protected BasePlayerManager Player;
    protected BaseCharacterMovement playermovement;

    public int UpdateUIInterval;
    private int tick;

    protected virtual void Awake()
    {
        Player = Components.PlayerManager;
        playermovement = Components.CharacterMovement;
    }

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
