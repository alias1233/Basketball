using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeathManager : MonoBehaviour
{
    public static DeathManager Singleton { get; internal set; }
    public GameObject Ghost;

    private void Awake()
    {
        Singleton = this;
    }

    public void PossessGhost(Vector3 placeofdeath, Quaternion rotation)
    {
        Ghost.transform.position = placeofdeath;
        Ghost.transform.rotation = rotation;
        Ghost.SetActive(true);
    }

    public void UnpossessGhost()
    {
        Ghost.SetActive(false);
    }
}
