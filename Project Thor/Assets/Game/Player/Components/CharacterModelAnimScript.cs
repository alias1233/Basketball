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
    private GameObject Parent;

    private bool bWasSliding;

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
        }

        Vector3 Vel = playermovement.GetVelocity();
        float VelMagnitude = Vel.magnitude;

        anim.SetFloat("MoveSpeed", VelMagnitude);

        Vector2 VelXY = new Vector2(Vel.x, Vel.z);
        Vector2 TransformXY = new Vector2(transform.forward.x, transform.forward.z);
        float Dir = Vector2.Dot(TransformXY, VelXY);
        float Mag = VelMagnitude;

        if(Dir < 0)
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