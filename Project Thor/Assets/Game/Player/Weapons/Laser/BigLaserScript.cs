using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

public class BigLaserScript : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem BaseLaser;
    [SerializeField]
    private ParticleSystem OuterLaser;

    private ParticleSystem.MainModule BaseLaserModule;
    private ParticleSystem.MainModule OuterLaserModule;

    private void Start()
    {
        BaseLaserModule = BaseLaser.main;
        OuterLaserModule = OuterLaser.main;
    }

    public void ShootLaser(float size)
    {
        BaseLaserModule.startSizeY = size;
        OuterLaserModule.startSizeY = size;

        Quaternion ForwardRotation = transform.rotation;
        const float AngleToRadianFactor = Mathf.PI / 180;

        BaseLaserModule.startRotationX = (ForwardRotation.eulerAngles.x + 90) * AngleToRadianFactor;
        BaseLaserModule.startRotationY = ForwardRotation.eulerAngles.y * AngleToRadianFactor;
        BaseLaserModule.startRotationZ = ForwardRotation.eulerAngles.z * AngleToRadianFactor;

        OuterLaserModule.startRotationX = (ForwardRotation.eulerAngles.x + 90) * AngleToRadianFactor;
        OuterLaserModule.startRotationY = ForwardRotation.eulerAngles.y * AngleToRadianFactor;
        OuterLaserModule.startRotationZ = ForwardRotation.eulerAngles.z * AngleToRadianFactor;

        BaseLaser.Play();
        OuterLaser.Play();
    }
}
