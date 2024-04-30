using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterModelAnimScript : MonoBehaviour
{
    private Transform SelfTransform;

    [SerializeField]
    private V1Movement playermovement;
    [SerializeField]
    private Animator anim;
    [SerializeField]
    private Animator WingAnim;
    [SerializeField]
    private Transform Parent;

    private bool bWasSliding;
    private bool bWasFlying;

    private void Awake()
    {
        SelfTransform = transform;
    }

    private void FixedUpdate()
    {
        if (playermovement.GetIsDead())
        {
            return;
        }

        if (playermovement.GetIsSliding())
        {
            if (!bWasSliding)
            {
                anim.SetBool("bSliding", true);

                bWasSliding = true;
            }

            return;
        }

        if (bWasSliding)
        {
            anim.SetBool("bSliding", false);

            SelfTransform.rotation = Parent.rotation;

            bWasSliding = false;

            return;
        }

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

        Vector3 Vel = playermovement.GetVelocity();
        Vector2 VelXY = new Vector2(Vel.x, Vel.z);

        anim.SetFloat("MoveSpeed", VelXY.magnitude);

        if (Vector2.Dot(new Vector2(SelfTransform.forward.x, SelfTransform.forward.z), VelXY) < 0)
        {
            anim.SetFloat("MoveFactor", -VelXY.magnitude);

            return;
        }

        anim.SetFloat("MoveFactor", VelXY.magnitude);
    }

    public void Die()
    {
        anim.SetTrigger("Die");
    }
}