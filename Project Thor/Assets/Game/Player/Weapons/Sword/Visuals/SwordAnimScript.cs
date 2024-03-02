using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordAnimScript : MonoBehaviour
{
    private Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void SwingAnim()
    {
        animator.SetTrigger("StartSwing");
    }
}
