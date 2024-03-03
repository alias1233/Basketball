using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAnimScript : MonoBehaviour
{
    public Animator animator;
    public bool IsActive;

    private void Update()
    {
        if(!IsActive)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        IsActive = true;
    }
    public void PunchAnim()
    {
        animator.SetTrigger("StartPunch");
    }
}
