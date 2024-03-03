using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TESTING : MonoBehaviour
{
    public int Times = 1000000;

    public bool FIRST = true;
    public int wow;

    public Vector3 MoveDirection;
    public Quaternion Rotation;
    public Quaternion ForwardRotation;

    public Transform TPOrientation;

    // Update is called once per frame
    void Update()
    {
        if (FIRST)
        {
            Inputs bruh = new Inputs(Times, transform.rotation, true, false, false, false, false, false, false);

            for (int i = 0; i < Times; i++)
            {
                Test(bruh);
            }

            return;
        }

        Inputs bruh1 = new Inputs(Times, transform.rotation, true, false, false, false, false, false, false);

        for (int i = 0; i < Times; i++)
        {
            Test2(bruh1);
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
