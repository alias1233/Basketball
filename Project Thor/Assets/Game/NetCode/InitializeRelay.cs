using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using System.Threading.Tasks;

public class InitializeRelay : MonoBehaviour
{
    public static InitializeRelay Instance { get; private set; }

    [SerializeField]
    MainMenuScript mainmenuscript;

    private void Awake()
    {
        Instance = this;
    }

    public void StartClient(string joinCode)
    {
        joinrelay(joinCode);
    }

    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log(" signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        var regionsTask = await Relay.Instance.ListRegionsAsync();

        mainmenuscript.InitRegions(regionsTask);
    }

    public async Task<string> CreateRelay(string region)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(6, region);
            string joincode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            unityTransport.SetRelayServerData(new RelayServerData(allocation, "wss"));
            unityTransport.UseWebSockets = true;

            //unityTransport.SetRelayServerData(new RelayServerData(allocation, "udp"));

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
            var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            unityTransport.SetRelayServerData(new RelayServerData(joinAllocation, "wss"));
            unityTransport.UseWebSockets = true;

            //unityTransport.SetRelayServerData(new RelayServerData(joinAllocation, "udp"));

            NetworkManager.Singleton.StartClient();
        }

        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}