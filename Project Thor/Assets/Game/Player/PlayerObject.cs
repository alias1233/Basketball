using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerObject : NetworkBehaviour
{
    public GameObject PickCharacterUI;

    [HideInInspector]
    public Teams Team;

    private NetworkObject PlayerCharacterNetworkObject;

    private BasePlayerManager PlayerCharacter;

    public override void OnNetworkSpawn()
    {
        if(!IsOwner)
        {
            PickCharacterUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            if (PickCharacterUI.activeSelf)
            {
                PickCharacterUI.SetActive(false);

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            else
            {
                PickCharacterUI.SetActive(true);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void V1()
    {
        PickCharacter(Characters.V1);
    }

    public void BigBertha()
    {
        PickCharacter(Characters.BigBertha);
    }

    private void PickCharacter(Characters character)
    {
        if (LobbyManager.Instance)
        {
            LobbyManager.Instance.DisableLobby();
        }

        ChangeCharacterServerRpc(character);

        PickCharacterUI.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    [ServerRpc]
    private void ChangeCharacterServerRpc(Characters character)
    {
        if(PlayerCharacterNetworkObject)
        {
            PlayerCharacterNetworkObject.Despawn(true);
        }

        GameObject newPlayer = Instantiate(GameManager.Singleton.CharacterList[(int)character], GameManager.Singleton.GetSpawnLocation(Team), Quaternion.identity);

        PlayerCharacterNetworkObject = newPlayer.GetComponent<NetworkObject>();
        PlayerCharacterNetworkObject.SpawnAsPlayerObject(OwnerClientId);
        PlayerCharacter = newPlayer.GetComponent<BasePlayerManager>();
    }

    public BasePlayerManager GetPlayerCharacter()
    {
        return PlayerCharacter;
    }
}
