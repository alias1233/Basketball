using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadingBulletScript : MonoBehaviour
{
    private LineRenderer tracer;

    public float BulletLifetime;

    private float OriginalWidth;
    private float TimeElapsed;
    private float DeltaTime;

    private void Awake()
    {
        tracer = GetComponent<LineRenderer>();
        OriginalWidth = tracer.endWidth;
        DeltaTime = Time.fixedDeltaTime;
    }

    private void OnEnable()
    {
        Invoke(nameof(DisableBullet), BulletLifetime);
        tracer.endWidth = OriginalWidth;
        tracer.startWidth = OriginalWidth;
    }

    private void DisableBullet()
    {
        gameObject.SetActive(false);
        TimeElapsed = 0;
    }

    private void FixedUpdate()
    {
        TimeElapsed += DeltaTime;
        float Width = (1 - (TimeElapsed / BulletLifetime)) * OriginalWidth;
        tracer.endWidth = Width;
        tracer.startWidth = Width;
    }
}
