using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraVisualsScript : MonoBehaviour
{
    private Transform selftransform;

    [SerializeField]
    private Transform CameraTransform;
    [SerializeField]
    private Camera PlayerCamera;

    private float OriginalFOV;

    private Vector3 OriginalPosition = new Vector3(0, 0.6f, 0);

    private void Awake()
    {
        selftransform = transform;
    }

    private void Start()
    {
        OriginalFOV = PlayerCamera.fieldOfView;
    }

    public void ChangePosition(Vector3 offset)
    {
        selftransform.localPosition = OriginalPosition + offset;
    }

    public void ResetPosition()
    {
        selftransform.localPosition = OriginalPosition;
    }

    public void Tilt(float amount)
    {
        CameraTransform.localRotation = Quaternion.RotateTowards(CameraTransform.localRotation, Quaternion.Euler(0, 0, amount), Mathf.Clamp(Mathf.Abs(amount / 2), 0.5f, 5f));
    }

    public IEnumerator ChangeFOV(float FOVdiff, float duration)
    {
        float elapsed = 0;
        float TargetFOV = FOVdiff + OriginalFOV;

        while (elapsed < duration)
        {
            PlayerCamera.fieldOfView = Mathf.Lerp(TargetFOV, OriginalFOV, elapsed / duration);

            elapsed += Time.deltaTime;

            yield return null;
        }

        PlayerCamera.fieldOfView = OriginalFOV;
    }

    public IEnumerator Shake(float Magnitude, float duration)
    {
        float elapsed = 0;

        while (elapsed < duration)
        {
            CameraTransform.localPosition = new Vector3((Mathf.PerlinNoise1D(elapsed * 25) - 0.5f) * Magnitude, (Mathf.PerlinNoise1D(elapsed * 25) - 0.5f) * Magnitude + 0.6f, 0);

            elapsed += Time.deltaTime;

            yield return null;
        }

        CameraTransform.localPosition = Vector3.zero;
    }
}
