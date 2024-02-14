using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AllPlayerInformationScript : MonoBehaviour
{
    public TMP_Text PlayerInfoText;
    public GameObject PlayerInfoObject;

    // Update is called once per frame
    void FixedUpdate()
    {
        if(!Input.GetKey(KeyCode.Tab))
        {
            PlayerInfoObject.SetActive(false);

            return;
        }

        if (!PlayerInfoObject.activeSelf)
        {
            PlayerInfoObject.SetActive(true);

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
