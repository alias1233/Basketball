using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using TMPro;

public class MainMenuScript : MonoBehaviour
{
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
    private InitializeRelay RelayIniter;
    [SerializeField]
    private TMP_Text JoinCode;

    private void Awake()
    {
        EnterButton.onClick.AddListener(() =>
        {
            StartClient(joincodetext.text);
        });
    }

    public async void StartHost()
    {
        string Text = await RelayIniter.StartHost();

        JoinCode.text = Text;
    }

    public void StartClient(string joinCode)
    {
        RelayIniter.StartClient(joinCode);
    }
}
