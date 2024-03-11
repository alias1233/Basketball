using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    //private LineRenderer tracer;

    //private float OriginalWidth;

    //private float TimeStart;

    public float BulletLifetime;

    //private void Awake()
    //{
        //tracer = GetComponent<LineRenderer>();
        //OriginalWidth = tracer.endWidth;
    //}

    private void OnEnable()
    {
        Invoke(nameof(DisableBullet), BulletLifetime);
        //tracer.endWidth = OriginalWidth;
        //TimeStart = Time.time;
    }

    private void DisableBullet()
    {
        gameObject.SetActive(false);
    }

    //private void Update()
    //{
        //tracer.endWidth = (1 - ((Time.time - TimeStart) / BulletLifetime)) * OriginalWidth;
    //}
}
