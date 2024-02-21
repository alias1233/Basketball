using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField]
    private Camera PlayerCamera;

    public float DashFOVOffset = 20;
    public float DashFOVDuration;

    private bool bIsChangingFOV;
    private float StartChangeFOVTime;
    private float FOV;
    private float DashFOV;

    public float Sens;
    public Transform Orientation;

    float xRotation;
    float yRotation;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        FOV = PlayerCamera.fieldOfView;
        DashFOV = FOV + DashFOVOffset;
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * Sens;
        float mouseY = -Input.GetAxis("Mouse Y") * Sens;

        yRotation += mouseX;
        xRotation += mouseY;

        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        Orientation.rotation = Quaternion.Euler(0, yRotation, 0);

        if(bIsChangingFOV)
        {
            if (Time.time - StartChangeFOVTime >= DashFOVDuration)
            {
                bIsChangingFOV = false;
                PlayerCamera.fieldOfView = FOV;

                return;
            }

            PlayerCamera.fieldOfView = Mathf.Lerp(DashFOV, FOV, (Time.time - StartChangeFOVTime) / DashFOVDuration);
        }
    }

    public void ChangeFOVDash()
    {
        bIsChangingFOV = true;
        StartChangeFOVTime = Time.time;
    }
}
