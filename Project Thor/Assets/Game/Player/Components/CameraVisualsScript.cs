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
    private float MoveCammeraDuration;

    private bool bMoveCammera;
    private float StartMoveCammeraTime;
    private Vector3 OriginalPos;
    private Vector3 ChangingPos;
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
        if (bMoveCammera)
        {
            if (Time.time - StartMoveCammeraTime >= MoveCammeraDuration)
            {
                bMoveCammera = false;
                CameraTransform.position = transform.position + ChangingPos;
                FPAllTransform.position = transform.position + ChangingPos;
            }

            else
            {
                Vector3 Pos = Vector3.Lerp(transform.position + OriginalPos, transform.position + ChangingPos, (Time.time - StartMoveCammeraTime) / MoveCammeraDuration);
                CameraTransform.position = Pos;
                FPAllTransform.position = Pos;
            }
        }
    }
    */

    public void ChangePosition(Vector3 offset, float duration)
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

    public void ResetPosition(float duration)
    {
        /*
        bMoveCammera = true;
        MoveCammeraDuration = duration;
        OriginalPos = CameraTransform.position - transform.position;
        ChangingPos = Vector3.zero;
        StartMoveCammeraTime = Time.time;
        */

        CameraTransform.position = transform.position;
        FPAllTransform.position = transform.position;
    }

    public void ChangeFOV(float FOVdiff, float duration)
    {
        bIsChangingFOV = true;
        DashFOVDuration = duration;
        ChangingFOV = OriginalFOV + FOVdiff;
        StartChangeFOVTime = Time.time;
    }
}
