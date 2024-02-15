using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterModelAnimScript : MonoBehaviour
{
    [SerializeField]
    private PlayerMovement playermovement;
    private Animator anim;

    private void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void FixedUpdate()
    {
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
}