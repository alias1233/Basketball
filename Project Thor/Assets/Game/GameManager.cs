using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using static ConnectionNotificationManager;

public enum Teams
{ 
    Red,
    Blue
}


public class GameManager : NetworkBehaviour
{
    public static GameManager Singleton { get; internal set; }

    [SerializeField]
    private GameObject PlayerPrefab;

    [SerializeField]
    private Transform RedTeamSpawn;
    [SerializeField]
    private Transform BlueTeamSpawn;

    [SerializeField]
    private Transform Graveyard;

    private NetworkList<PlayerInformation> PlayerList;

    private void Awake()
    {
        Singleton = this;

        PlayerList = new NetworkList<PlayerInformation>();
    }

    private void Start()
    {
        ConnectionNotificationManager.Singleton.OnClientConnectionNotification += UpdatePlayers;
    }



    private void UpdatePlayers(ulong clientId, ConnectionStatus connection)
    {
        if (!IsServer)
        {
            return;
        }

        if(connection == ConnectionStatus.Connected)
        {
            Teams team = Teams.Red;
            ushort val = (ushort)(clientId % 2);

            if (val == 1)
            {
                team = Teams.Blue;
            }

            PlayerInformation playerinfo = new PlayerInformation(clientId, team);

            PlayerList.Add(playerinfo);

            GameObject newPlayer = Instantiate(PlayerPrefab);

            if (team == Teams.Red)
            {
                newPlayer.transform.position = RedTeamSpawn.position;
            }

            if (team == Teams.Blue)
            {
                newPlayer.transform.position = BlueTeamSpawn.position;
            }

            NetworkObject PlayerNetworkObject = newPlayer.GetComponent<NetworkObject>();
            PlayerNetworkObject.SpawnAsPlayerObject(clientId);
        }
    }

    public List<PlayerInformation> GetAllPlayerInformation()
    {
        List<PlayerInformation> playerlist = new List<PlayerInformation>();

        foreach (var i in PlayerList)
        {
            playerlist.Add(i);
        }

        return playerlist;
    }

    public Vector3 GetSpawnLocation(Teams team)
    {
        if (team == Teams.Red)
        {
            return RedTeamSpawn.position;
        }

        if (team == Teams.Blue)
        {
            return BlueTeamSpawn.position;
        }

        return Vector3.zero;
    }

    public Vector3 GetGraveyardLocation()
    {
        return Graveyard.position;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
    }
}