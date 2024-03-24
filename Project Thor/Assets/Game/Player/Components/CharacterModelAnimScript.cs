using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterModelAnimScript : MonoBehaviour
{
    [SerializeField]
    private PlayerMovement playermovement;
    [SerializeField]
    private Animator anim;
    [SerializeField]
    private Animator WingAnim;
    [SerializeField]
    private GameObject Parent;

    private bool bWasSliding;
    private bool bWasFlying;

    private void FixedUpdate()
    {
        if(playermovement.GetIsSliding())
        {
            if(!bWasSliding)
            {
                anim.SetBool("bSliding", true);

                bWasSliding = true;
            }

            return;
        }

        if(bWasSliding)
        {
            anim.SetBool("bSliding", false);

            transform.rotation = Parent.transform.rotation;

            bWasSliding = false;

            return;
        }

        if(playermovement.GetIsFlying())
        {
            if(!bWasFlying)
            {
                WingAnim.SetInteger("Mode", 2);
                anim.SetFloat("MoveSpeed", 2);
                anim.SetFloat("MoveFactor", 1);

                bWasFlying = true;
            }

            WingAnim.SetFloat("FlightSpeed", 1 + playermovement.GetVelocity().magnitude / 10);

            return;
        }

        if(bWasFlying)
        {
            bWasFlying = false;

            WingAnim.SetInteger("Mode", 1);
        }

        Vector3 Vel = playermovement.GetVelocity();
        float VelMagnitude = Vel.magnitude;

        anim.SetFloat("MoveSpeed", VelMagnitude);

        Vector2 VelXY = new Vector2(Vel.x, Vel.z);
        Vector2 TransformXY = new Vector2(transform.forward.x, transform.forward.z);
        float Dir = Vector2.Dot(TransformXY, VelXY);
        float Mag = VelMagnitude;

        if (Dir < 0)
        {
            Mag = -Mag;
        }

        anim.SetFloat("MoveFactor", Mag);
    }

    private void LateUpdate()
    {
        if (bWasSliding)
        {
            transform.rotation = Quaternion.LookRotation(playermovement.GetVelocity(), Vector3.up);
        }
    }
}