using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAnimScript : MonoBehaviour
{
    public Animator animator;

    public void PunchAnim()
    {
        animator.SetTrigger("StartPunch");
    }

    public void HoldBall()
    {
        animator.SetBool("bHoldingBall", true);
    }

    public void UnholdBall()
    {
        animator.SetBool("bHoldingBall", false);
    }

    public void EnterDunk()
    {
        animator.SetBool("bDunking", true);
    }

    public void ExitDunk()
    {
        animator.SetBool("bDunking", false);
    }
}
