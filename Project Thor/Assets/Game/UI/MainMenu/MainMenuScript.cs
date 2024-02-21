using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

public class MainMenuScript : MonoBehaviour
{
    [SerializeField]
    GameObject CreateAndJoinGame;
    [SerializeField]
    private Button ServerButton;
    [SerializeField]
    private Button ClientButton;
    [SerializeField]
    private Button HostButton;
    [SerializeField]
    private Button EnterButton;
    [SerializeField]
    private TMP_InputField joincodetext;
    [SerializeField]
    private TMP_Text JoinCode;
    [SerializeField]
    private TMP_Dropdown RegionSelector;
    [SerializeField]
    private GameObject MainMenuCameraObject;

    private List<Region> Regions;
    private string RegionId;

    private void Awake()
    {
        EnterButton.onClick.AddListener(() =>
        {
            StartClient(joincodetext.text);
        });
    }

    public void InitRegions(List<Region> regions)
    {
        Regions = regions;
        List<string> RegionNames = new List<string>();

        RegionNames.Add("Select Region");

        foreach (Region region in regions)
        {
            RegionNames.Add(region.Id);
        }

        RegionSelector.AddOptions(RegionNames);
    }

    public async void StartHost()
    {
        if(RegionSelector.value != 0)
        {
            foreach (Region region in Regions)
            {
                if (region.Id.Equals(Regions[RegionSelector.value - 1].Id))
                {
                    RegionId = region.Id;

                    break;
                }
            }
        }

        string Text = await InitializeRelay.Instance.CreateRelay(RegionId);

        JoinCode.text = Text;

        CreateAndJoinGame.SetActive(false);

        MainMenuCameraObject.SetActive(false);
    }

    public void StartClient(string joinCode)
    {
        InitializeRelay.Instance.StartClient(joinCode);

        JoinCode.text = joinCode;

        CreateAndJoinGame.SetActive(false);

        MainMenuCameraObject.SetActive(false);
    }
}
