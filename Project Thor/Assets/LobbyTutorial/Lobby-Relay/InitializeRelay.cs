using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using System.Threading.Tasks;

public class InitializeRelay : MonoBehaviour
{
    public static InitializeRelay Instance { get; private set; }

    [SerializeField]
    LobbyUI lobbyUI;

    private void Awake()
    {
        Instance = this;
    }

    public async void GetRegions()
    {
        var regionsTask = await Relay.Instance.ListRegionsAsync();

        lobbyUI.InitRegions(regionsTask);
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
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joincode);
            var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();

            unityTransport.SetRelayServerData(new RelayServerData(joinAllocation, "wss"));
            unityTransport.UseWebSockets = true;

            NetworkManager.Singleton.StartClient();
        }

        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}