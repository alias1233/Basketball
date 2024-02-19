using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUIScript : MonoBehaviour
{
    public GameObject PlayerUI;
    public GameObject SettingsObject;
    public TMP_Text PlayerInfoText;
    public Slider SensitivitySlider;
    public CameraScript PlayerCameraScript;

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
                PlayerUI.SetActive(true);

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

                string players = "";
                List<PlayerInformation> playerlist = GameManager.Singleton.GetAllPlayerInformation();
                bool bruh = true;

                foreach (var i in playerlist)
                {
                    if (bruh)
                    {
                        bruh = false;
                    }

                    else
                    {
                        players += ", ";
                    }

                    players += i.Id.ToString() + ": ";

                    if (i.Team == 0)
                    {
                        players += "red";
                    }

                    else
                    {
                        players += "blue";
                    }
                }

                PlayerInfoText.text = players;
            }
        }
    }
}
