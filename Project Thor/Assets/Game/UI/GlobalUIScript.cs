using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GlobalUIScript : MonoBehaviour
{
    public static GlobalUIScript Singleton;

    public event Action<bool> OnSettingsUIChangeActive;

    public GameObject SettingsObject;
    public Slider SensitivitySlider;

    [SerializeField]
    private TMP_Text RedTeamPlayers;
    [SerializeField]
    private TMP_Text BlueTeamPlayers;

    public GameObject Letter1;
    public GameObject Letter2;
    public GameObject Letter3;
    public AudioSource DomainExpansionVoice;
    public AudioSource Boom1;
    public AudioSource Boom2;

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

    public void ActivateDomain()
    {
        Letter1.SetActive(true);
        DomainExpansionVoice.Play();

        Invoke(nameof(ShowLetter2), 0.5f);
        Invoke(nameof(ShowLetter3), 1f);
        Invoke(nameof(EndDomain), 2);
    }

    private void ShowLetter2()
    {
        Letter2.SetActive(true);
        Boom1.Play();
    }

    private void ShowLetter3()
    {
        Letter3.SetActive(true);
        Boom2.Play();
    }

    private void EndDomain()
    {
        Letter1.SetActive(false);
        Letter2.SetActive(false);
        Letter3.SetActive(false);
    }
}
