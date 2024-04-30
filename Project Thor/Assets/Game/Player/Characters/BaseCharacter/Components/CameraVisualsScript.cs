using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraVisualsScript : MonoBehaviour
{
    private Transform selftransform;

    [SerializeField]
    private Transform CameraTransform;
    [SerializeField]
    private Transform ParentCameraTransform;
    [SerializeField]
    private Transform CameraParentParentTransform;
    [SerializeField] 
    private Transform CameraParentParent;
    [SerializeField]
    private Transform FPCameraParent;
    [SerializeField]
    private Camera PlayerCamera;
    [SerializeField]
    private Camera OverlayCamera;

    private bool bThirdPerson;

    private float OriginalFOV;

    private Vector3 OriginalPosition;

    public Vector3 ThirdPersonOffset;

    private void Awake()
    {
        selftransform = transform;

        OriginalFOV = PlayerCamera.fieldOfView;
        OriginalPosition = selftransform.localPosition;

        timeholderx = Mathf.PI;
    }

    public IEnumerator EnterThirdPerson(float duration)
    {
        bThirdPerson = true;
        float elapsed = 0;

        while (elapsed < duration)
        {
            CameraParentParent.localPosition = Vector3.Lerp(Vector3.zero, ThirdPersonOffset, elapsed / duration);

            elapsed += Time.deltaTime;

            yield return null;
        }

        CameraParentParent.localPosition = ThirdPersonOffset;
    }

    public IEnumerator EnterFirstPerson(float duration)
    {
        bThirdPerson = false;
        PlayerCamera.fieldOfView = OriginalFOV;
        OverlayCamera.fieldOfView = OriginalFOV;
        float elapsed = 0;

        while (elapsed < duration)
        {
            CameraParentParent.localPosition = Vector3.Lerp(ThirdPersonOffset, Vector3.zero, elapsed / duration);

            elapsed += Time.deltaTime;

            yield return null;
        }

        CameraParentParent.localPosition = Vector3.zero;
    }

    public void ChangePosition(Vector3 offset)
    {
        if(bThirdPerson)
        {
            return;
        }

        selftransform.localPosition = OriginalPosition + offset;
    }

    public void ResetPosition()
    {
        if (bThirdPerson)
        {
            return;
        }

        selftransform.localPosition = OriginalPosition;
    }

    private float timeholdery;
    private float timeholderx;

    public void HeadBob(float Speed, float Amplitude)
    {
        CameraTransform.localPosition = new Vector3(Mathf.Cos(timeholderx) * Amplitude * 2, Mathf.Sin(timeholdery) * Amplitude, 0);

        timeholdery += Speed;
        timeholderx += Speed / 2;
    }

    public void ResetHeadBob(float Speed)
    {
        if(CameraTransform.localPosition == Vector3.zero)
        {
            timeholdery = 0;
            timeholderx = Mathf.PI / 2;

            return;
        }

        CameraTransform.localPosition = Vector3.MoveTowards(CameraTransform.localPosition, Vector3.zero, Speed);
    }

    public void StabilizeCamera()
    {
        CameraTransform.LookAt(selftransform.position + selftransform.forward * 50, FPCameraParent.up);
    }

    public void Tilt(float amount)
    {
        CameraParentParent.localRotation = Quaternion.RotateTowards(CameraParentParent.localRotation, Quaternion.Euler(0, 0, amount), Mathf.Clamp(Mathf.Abs(amount / 5), 0.5f, 5f));
    }

    public void TiltY(float amount, float speed)
    {
        ParentCameraTransform.localRotation = Quaternion.RotateTowards(ParentCameraTransform.localRotation, Quaternion.Euler(Mathf.Clamp(amount, 0, 10000), 0, 0), speed);
    }

    public void OffsetSmooth(Vector3 MaxOffset, float Speed)
    {
        selftransform.localPosition = Vector3.MoveTowards(selftransform.localPosition, MaxOffset, Speed);
    }

    public void ResetPositionSmooth(float Speed)
    {
        selftransform.localPosition = Vector3.MoveTowards(selftransform.localPosition, OriginalPosition, Speed);
    }

    public void OffsetSmoothPosRot(Vector3 MaxOffset, float Speed, float RotAmount, float RotSpeed)
    {
        selftransform.localPosition = Vector3.MoveTowards(selftransform.localPosition, MaxOffset, Speed);
        FPCameraParent.localRotation = Quaternion.RotateTowards(FPCameraParent.localRotation, Quaternion.Euler(0, 0, RotAmount), RotSpeed);
    }

    public void ResetPositionSmoothPosRot(float Speed, float RotSpeed)
    {
        selftransform.localPosition = Vector3.MoveTowards(selftransform.localPosition, OriginalPosition, Speed);
        FPCameraParent.localRotation = Quaternion.RotateTowards(FPCameraParent.localRotation, Quaternion.identity, RotSpeed);
    }

    public void ChangeFOVSmooth(float amount, float speed)
    {
        if(amount + OriginalFOV > PlayerCamera.fieldOfView)
        {
            speed *= 2;
        }

        float NewFOV = Mathf.MoveTowards(PlayerCamera.fieldOfView, OriginalFOV + amount, speed);

        PlayerCamera.fieldOfView = NewFOV;
        OverlayCamera.fieldOfView = NewFOV;
    }

    public void Offset(float amount)
    {
        CameraParentParent.localPosition = Vector3.MoveTowards(CameraParentParent.localPosition, ThirdPersonOffset + ThirdPersonOffset.normalized * amount * 0.15f, 1f);

        float NewFOV = Mathf.MoveTowards(PlayerCamera.fieldOfView, OriginalFOV + amount, 1f);

        PlayerCamera.fieldOfView = NewFOV;
        OverlayCamera.fieldOfView = NewFOV;
    }

    public IEnumerator ChangeFOV(float FOVdiff, float duration)
    {
        float elapsed = 0;
        float TargetFOV = FOVdiff + OriginalFOV;

        while (elapsed < duration)
        {
            float NewFOV = Mathf.Lerp(TargetFOV, OriginalFOV, elapsed / duration);

            PlayerCamera.fieldOfView = NewFOV;
            OverlayCamera.fieldOfView = NewFOV;

            elapsed += Time.deltaTime;

            yield return null;
        }

        PlayerCamera.fieldOfView = OriginalFOV;
        OverlayCamera.fieldOfView = OriginalFOV;
    }

    public IEnumerator Shake(float Magnitude, float duration)
    {
        if (bThirdPerson)
        {
            yield break;
        }

        float elapsed = 0;

        while (elapsed < duration)
        {
            if (bThirdPerson)
            {
                yield break;
            }

            CameraParentParentTransform.localPosition = new Vector3((Mathf.PerlinNoise1D(elapsed * 25) - 0.5f) * Magnitude, (Mathf.PerlinNoise1D(elapsed * 25) - 0.5f) * Magnitude + 0.6f, 0);

            elapsed += Time.deltaTime;

            yield return null;
        }

        CameraParentParentTransform.localPosition = Vector3.zero;
    }
}
