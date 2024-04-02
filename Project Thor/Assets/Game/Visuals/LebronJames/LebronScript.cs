using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LebronScript : MonoBehaviour
{
    public AudioSource YouAreMySunshine;

    private Transform Crash;

    private void OnEnable()
    {
        YouAreMySunshine.Play();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.position.y < -50)
        {
            transform.position = Crash.position;
        }

        Destroy(other.gameObject);
    }
}
