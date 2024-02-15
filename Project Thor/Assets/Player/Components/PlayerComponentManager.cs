using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerComponentManager : NetworkBehaviour
{
    public List<MonoBehaviour> DisabledForOwnerScripts;
    public List<MonoBehaviour> DisabledForOthersScripts;

    public Camera PlayerCamera;
    public AudioListener PlayerAudioListener;

    public GameObject CharacterModel;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            foreach(var i in DisabledForOwnerScripts)
            {
                i.enabled = false;
            }

            CharacterModel.SetActive(false);

            return;
        }

        foreach (var i in DisabledForOthersScripts)
        {
            i.enabled = false;
        }

        PlayerCamera.enabled = false;
        PlayerAudioListener.enabled = false;
    }
}
