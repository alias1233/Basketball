using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraVisualsScript : MonoBehaviour
{
    [SerializeField]
    private Transform ParentTransform;

    private Camera PlayerCamera;

    private float DashFOVDuration;

    private bool bIsChangingFOV;
    private float StartChangeFOVTime;
    private float OriginalFOV;
    private float ChangingFOV;

    private float MoveCammeraDuration;

    private bool bMoveCammera;
    private float StartMoveCammeraTime;
    private Vector3 OriginalPos;
    private Vector3 ChangingPos;

    private void Start()
    {
        PlayerCamera = GetComponent<Camera>();

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

    private void FixedUpdate()
    {
        if (bMoveCammera)
        {
            if (Time.time - StartMoveCammeraTime >= MoveCammeraDuration)
            {
                bMoveCammera = false;
                transform.position = ParentTransform.position + ChangingPos;
            }

            else
            {
                transform.position = Vector3.Lerp(ParentTransform.position + OriginalPos, ParentTransform.position + ChangingPos, (Time.time - StartMoveCammeraTime) / MoveCammeraDuration);
            }
        }
    }

    public void ChangePosition(Vector3 offset, float duration)
    {
        bMoveCammera = true;
        MoveCammeraDuration = duration;
        OriginalPos = transform.position - ParentTransform.position;
        ChangingPos = offset;
        StartMoveCammeraTime = Time.time;
    }

    public void ResetPosition(float duration)
    {
        bMoveCammera = true;
        MoveCammeraDuration = duration;
        OriginalPos = transform.position - ParentTransform.position;
        ChangingPos = Vector3.zero;
        StartMoveCammeraTime = Time.time;
    }

    public void ChangeFOV(float FOVdiff, float duration)
    {
        bIsChangingFOV = true;
        DashFOVDuration = duration;
        ChangingFOV = OriginalFOV + FOVdiff;
        StartChangeFOVTime = Time.time;
    }
}
