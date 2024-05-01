using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class V1AnimScript : BaseCharacterModelAnimScript
{
    private V1Movement playermovement;
    [SerializeField]
    private Animator WingAnim;

    private bool bWasFlying;

    protected override void Awake()
    {
        base.Awake();

        playermovement = (V1Movement)PlayerMovement;
    }

    protected override void UpdateAnimations()
    {
        if (playermovement.GetIsFlying())
        {
            if (!bWasFlying)
            {
                WingAnim.SetInteger("Mode", 2);
                anim.SetFloat("MoveSpeed", 2);
                anim.SetFloat("MoveFactor", 1);

                bWasFlying = true;
            }

            WingAnim.SetFloat("FlightSpeed", 1 + playermovement.GetVelocity().magnitude / 15);

            return;
        }

        if (bWasFlying)
        {
            bWasFlying = false;

            WingAnim.SetInteger("Mode", 1);
        }

        base.UpdateAnimations();
    }
}