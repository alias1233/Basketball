using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUIScript : MonoBehaviour
{
    public PlayerManager Player;
    public GameObject PlayerUI;
    public GameObject SettingsObject;
    public Slider SensitivitySlider;
    public CameraScript PlayerCameraScript;

    [SerializeField]
    TMP_Text RedTeamPlayers;
    [SerializeField]
    TMP_Text BlueTeamPlayers;

    private void Start()
    {
        SettingsObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (SettingsObject.activeSelf)
            {
                PlayerCameraScript.enabled = true;
                PlayerCameraScript.Sens = SensitivitySlider.value;

                SettingsObject.SetActive(false);

                if(!Player.GetIsDead())
                {
                    PlayerUI.SetActive(true);
                }

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            else
            {
                PlayerUI.SetActive(false);
                SettingsObject.SetActive(true);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                PlayerCameraScript.enabled = false;

                DisplayPlayers();
            }
        }
    }

    private void DisplayPlayers()
    {
        string RedTeam = "";
        string BlueTeam = "";
        List<PlayerInformation> playerlist = GameManager.Singleton.GetAllPlayerInformation();

        foreach (var i in playerlist)
        {
            if(i.Team == Teams.Red)
            {
                RedTeam += i.Id;
            }

            if(i.Team == Teams.Blue)
            {
                BlueTeam += i.Id;
            }
        }

        RedTeamPlayers.text = RedTeam;
        BlueTeamPlayers.text = BlueTeam;
    }
}
