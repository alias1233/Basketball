using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCharacterModelAnimScript : MonoBehaviour
{
    protected Transform SelfTransform;

    public BaseCharacterComponents Components;

    protected BaseCharacterMovement PlayerMovement;
    [SerializeField]
    protected Animator anim;
    [SerializeField]
    protected Transform Parent;

    private bool bWasSliding;

    protected virtual void Awake()
    {
        SelfTransform = transform;

        PlayerMovement = Components.CharacterMovement;
    }

    private void FixedUpdate()
    {
        if (PlayerMovement.GetIsDead())
        {
            return;
        }

        if (PlayerMovement.GetIsSliding())
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

        UpdateAnimations();
    }

    protected virtual void UpdateAnimations()
    {
        Vector3 Vel = PlayerMovement.GetVelocity();
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
