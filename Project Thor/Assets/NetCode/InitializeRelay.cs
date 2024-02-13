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

public class InitializeRelay : MonoBehaviour
{
    public static InitializeRelay Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public async Task<string> StartHost()
    {
        return await CreateRelay();
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
    }

    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(6);
            string joincode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            var unityTransport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            unityTransport.SetRelayServerData(new RelayServerData(allocation, "wss"));
            unityTransport.UseWebSockets = true;

            //unityTransport.SetRelayServerData(new RelayServerData(allocation, "udp"));

            /*
            unityTransport.SetHostRelayData(
              allocation.RelayServer.IpV4,
              (ushort)allocation.RelayServer.Port,
              allocation.AllocationIdBytes,
              allocation.Key,
              allocation.ConnectionData,
              true
            );
            */

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

            //unityTransport.SetRelayServerData(new RelayServerData(joinAllocation, "udp"));

            unityTransport.SetRelayServerData(new RelayServerData(joinAllocation, "wss"));
            unityTransport.UseWebSockets = true;

            /*
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
              joinAllocation.RelayServer.IpV4,
              (ushort)joinAllocation.RelayServer.Port,
              joinAllocation.AllocationIdBytes,
              joinAllocation.Key,
              joinAllocation.ConnectionData,
              joinAllocation.HostConnectionData,
              true
            );
            */

            NetworkManager.Singleton.StartClient();
        }

        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}