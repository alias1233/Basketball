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

    /*
    private void OnAnimatorIK(int layerIndex)
    {
        print("IK");

        anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, LeftFootWeight);
        anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, LeftFootRotationWeight);
        anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, RightFootWeight);
        anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, RightFootRotationWeight);

        RaycastHit hit;

        Ray ray = new Ray(anim.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);
        if (Physics.Raycast(ray, out hit, DistanceToGround + 1f, layerMask))
        {
            Vector3 footPosition = hit.point;
            footPosition.y += DistanceToGround;
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, footPosition);
            anim.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(transform.forward, hit.normal));
        }

        // Right Foot
        ray = new Ray(anim.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down);
        if (Physics.Raycast(ray, out hit, DistanceToGround + 1f, layerMask))
        {
            Vector3 footPosition = hit.point;
            footPosition.y += DistanceToGround;
            anim.SetIKPosition(AvatarIKGoal.RightFoot, footPosition);
            anim.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(transform.forward, hit.normal));
        }
    }
    */
}