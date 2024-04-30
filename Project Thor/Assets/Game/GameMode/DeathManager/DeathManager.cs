using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeathManager : MonoBehaviour
{
    public static DeathManager Singleton { get; internal set; }
    public GameObject Ghost;
    private GhostScript ghostscript;

    private void Awake()
    {
        Singleton = this;
        ghostscript = Ghost.GetComponent<GhostScript>();
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

    public void SetRespawnTime(int RespawnTime)
    {
        ghostscript.SetRespawnTime(RespawnTime);
    }
}
