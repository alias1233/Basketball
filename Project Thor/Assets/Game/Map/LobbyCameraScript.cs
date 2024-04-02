using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyCameraScript : MonoBehaviour
{
    private Transform SelfTransform;

    public float RotateSpeed;
    public Vector3 Center;

    private void Awake()
    {
        SelfTransform = transform;
    }

    private void Update()
    {
        SelfTransform.RotateAround(Center, Vector3.up, RotateSpeed * Time.deltaTime);
        SelfTransform.LookAt(Center);
    }
}
