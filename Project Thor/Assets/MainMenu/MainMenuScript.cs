using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
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
    public static MainMenuScript Instance { get; private set; }
    public TMP_InputField joincodetext;

    private void Awake()
    {
        Instance = this;

        ServerButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        });

        ClientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        });

        HostButton.onClick.AddListener(() =>
        {
            //NetworkManager.Singleton.StartHost();

            CreateRelay();
        });
    }

    private async void Start()
    {
        /*
         await UnityServices.InitializeAsync(); 
         AuthenticationService.Instance.SignedIn += () => 
         {
            Debug.Log(" signed in " + AuthenticationService.Instance.PlayerId);
            
         };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        */
    }

    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string joincode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joincode);

            RelayServerData relayServerData = new RelayServerData(allocation, "wss");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData
            (
              allocation.RelayServer.IpV4,
              (ushort)allocation.RelayServer.Port,
              allocation.AllocationIdBytes,
              allocation.Key,
              allocation.ConnectionData
            );
            NetworkManager.Singleton.StartHost();
            return joincode;
        }

        catch (RelayServiceException e)
        {
            Debug.Log(e);
            return null;

        }
    }

    public async void joinrelay(string joincode)
    {
        try
        {
            Debug.Log("join relay " + joincode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joincode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
              joinAllocation.RelayServer.IpV4,
               (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                 joinAllocation.Key,
                  joinAllocation.ConnectionData,
                  joinAllocation.HostConnectionData
        );
            Debug.Log("joined");
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}

/*   public async Task<string> CreateRelay()
{
   try
   {
       Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3);
       string joincode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
       Debug.Log(joincode);

       NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
           allocation.RelayServer.IpV4,
           (ushort)allocation.RelayServer.Port,
           allocation.AllocationIdBytes,
           allocation.Key,
           allocation.ConnectionData
       );

       NetworkManager.Singleton.StartHost();

       return joincode;
   }
   catch (RelayServiceException e)
   {
       Debug.Log(e);
       throw; // Throw the exception to indicate an error occurred
   }
}*/
