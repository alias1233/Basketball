using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class TESTING : MonoBehaviour
{
    public int Times = 1000000;

    public bool FIRST = true;
    public bool SECOND;
    private bool THIRD;
    public int wow;

    public Vector3 Velocity;
    public Vector3 Velocity2;

    private float DeltaTime;

    public Vector3 MoveDirection;
    public Quaternion Rotation;
    public Quaternion ForwardRotation;

    public Transform TPOrientation;

    RaycastHit[] Hits = new RaycastHit[5];

    Collider[] Colliders = new Collider[5];

    private Transform SelfTransform;

    private void Start()
    {
        SelfTransform = transform;
        DeltaTime = Time.fixedDeltaTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (FIRST)
        {
            for (int i = 0; i < Times; i++)
            {
                MoveDirection = Velocity * DeltaTime;
            }

            return;
        }

        for (int i = 0; i < Times; i++)
        {
            MoveDirection = Velocity2;
        }
    }

    private void Test(Inputs input)
    {
        Rotation = input.Rotation;

        float a = Mathf.Sqrt((Rotation.w * Rotation.w) + (Rotation.y * Rotation.y));
        ForwardRotation = new Quaternion(0, Rotation.y / a, 0, Rotation.w / a);

        TPOrientation.rotation = ForwardRotation;
        Vector3 bruh = ForwardRotation * Vector3.forward;

        if (input.W)
        {
            MoveDirection += bruh;
        }

        if (input.A)
        {
            MoveDirection -= bruh;
        }

        if (input.S)
        {
            MoveDirection -= bruh;
        }

        if (input.D)
        {
            MoveDirection += bruh;
        }

        MoveDirection.Normalize();
    }

    private void Test2(Inputs input)
    {
        Rotation = input.Rotation;

        float a = Mathf.Sqrt((Rotation.w * Rotation.w) + (Rotation.y * Rotation.y));
        ForwardRotation = new Quaternion(0, Rotation.y / a, 0, Rotation.w / a);

        TPOrientation.rotation = ForwardRotation;

        if (input.W)
        {
            MoveDirection += ForwardRotation * Vector3.forward;
        }

        if (input.A)
        {
            MoveDirection -= ForwardRotation * Vector3.forward;
        }

        if (input.S)
        {
            MoveDirection -= ForwardRotation * Vector3.forward;
        }

        if (input.D)
        {
            MoveDirection += ForwardRotation * Vector3.forward;
        }

        MoveDirection.Normalize();
    }
}
