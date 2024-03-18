using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraVisualsScript : MonoBehaviour
{
    [SerializeField]
    private Transform CameraTransform;
    [SerializeField]
    private Camera PlayerCamera;
    [SerializeField]
    private Transform FPAllTransform;

    private float DashFOVDuration;

    private bool bIsChangingFOV;
    private float StartChangeFOVTime;
    private float OriginalFOV;
    private float ChangingFOV;

    /*
    private float TiltCameraDuration;

    private bool bTiltCamera;
    private float StartTiltCameraTime;
    private Quaternion TiltRotation;
    private float TiltAmount;
    */

    private void Start()
    {
        OriginalFOV = PlayerCamera.fieldOfView;
    }

    private void Update()
    {
        if (bIsChangingFOV)
        {
            if (Time.time - StartChangeFOVTime >= DashFOVDuration)
            {
                bIsChangingFOV = false;
                PlayerCamera.fieldOfView = OriginalFOV;
            }

            else
            {
                PlayerCamera.fieldOfView = Mathf.Lerp(ChangingFOV, OriginalFOV, (Time.time - StartChangeFOVTime) / DashFOVDuration);
            }
        }
    }

    /*
    private void FixedUpdate()
    {
        if (bTiltCamera)
        {
            if (Time.time - StartTiltCameraTime >= TiltCameraDuration)
            {
                bTiltCamera = false;
                CameraTransform.localRotation = Quaternion.RotateTowards(CameraTransform.localRotation, TiltRotation, 0.5f);
            }

            else
            {
                //Vector3 Pos = Vector3.Lerp(transform.position + OriginalPos, transform.position + ChangingPos, (Time.time - StartMoveCammeraTime) / MoveCammeraDuration);
                //CameraTransform.position = Pos;
                //FPAllTransform.position = Pos;
            }
        }
    }
    */

    public void ChangePosition(Vector3 offset)
    {
        /*
        bMoveCammera = true;
        MoveCammeraDuration = duration;
        OriginalPos = CameraTransform.position - transform.position;
        ChangingPos = offset;
        StartMoveCammeraTime = Time.time;
        */

        CameraTransform.position = offset + transform.position;
        FPAllTransform.position = offset + transform.position;
    }

    public void ResetPosition()
    {
        CameraTransform.position = transform.position;
        FPAllTransform.position = transform.position;
    }

    public void Tilt(float amount)
    {
        CameraTransform.localRotation = Quaternion.RotateTowards(CameraTransform.localRotation, Quaternion.Euler(0, 0, amount), Mathf.Clamp(Mathf.Abs(amount / 2), 0.5f, 5f));
    }

    public void ChangeFOV(float FOVdiff, float duration)
    {
        bIsChangingFOV = true;
        DashFOVDuration = duration;
        ChangingFOV = OriginalFOV + FOVdiff;
        StartChangeFOVTime = Time.time;
    }
}
