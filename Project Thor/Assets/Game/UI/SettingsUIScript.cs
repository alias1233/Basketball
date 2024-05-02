using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUIScript : MonoBehaviour
{
    public static SettingsUIScript Singleton;

    public event Action<bool> OnSettingsUIChangeActive;

    public GameObject SettingsObject;
    public Slider SensitivitySlider;

    [SerializeField]
    private TMP_Text RedTeamPlayers;
    [SerializeField]
    private TMP_Text BlueTeamPlayers;

    private void Awake()
    {
        Singleton = this;
    }

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
                SettingsObject.SetActive(false);

                OnSettingsUIChangeActive?.Invoke(false);

                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            else
            {
                SettingsObject.SetActive(true);

                OnSettingsUIChangeActive?.Invoke(true);

                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

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

    public float GetSensitivity()
    {
        return SensitivitySlider.value;
    }
}
