using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    private Transform SelfTransform;

    public float Sens;

    float xRotation;
    float yRotation;

    private void Awake()
    {
        SelfTransform = transform;
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Update is called once per frame
    void Update()
    {
        yRotation += Input.GetAxis("Mouse X") * Sens;
        xRotation -= Input.GetAxis("Mouse Y") * Sens;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        SelfTransform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
    }
}
