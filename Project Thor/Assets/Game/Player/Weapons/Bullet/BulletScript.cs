using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletScript : MonoBehaviour
{
    public float BulletLifetime;

    private void OnEnable()
    {
        Invoke(nameof(DisableBullet), BulletLifetime);
    }

    private void DisableBullet()
    {
        gameObject.SetActive(false);
    }
}
