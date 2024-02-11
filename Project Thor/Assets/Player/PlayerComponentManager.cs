using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerComponentManager : NetworkBehaviour
{
    public GameObject PlayerCameraObject;
    public GameObject CharacterModel;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            PlayerCameraObject.SetActive(true);

            return;
        }

        CharacterModel.SetActive(true);
    }
}
