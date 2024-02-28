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

    public void ShootLaser(float size, Vector3 rotinvector)
    {
        BaseLaserModule.startSizeY = size;
        OuterLaserModule.startSizeY = size;

        Quaternion rotation = Quaternion.LookRotation(rotinvector, Vector3.up);
        const float AngleToRadianFactor = Mathf.PI / 180;

        BaseLaserModule.startRotationX = (rotation.eulerAngles.x + 90) * AngleToRadianFactor;
        BaseLaserModule.startRotationY = rotation.eulerAngles.y * AngleToRadianFactor;
        BaseLaserModule.startRotationZ = rotation.eulerAngles.z * AngleToRadianFactor;

        OuterLaserModule.startRotationX = (rotation.eulerAngles.x + 90) * AngleToRadianFactor;
        OuterLaserModule.startRotationY = rotation.eulerAngles.y * AngleToRadianFactor;
        OuterLaserModule.startRotationZ = rotation.eulerAngles.z * AngleToRadianFactor;

        BaseLaser.Play();
        OuterLaser.Play();
    }
}
