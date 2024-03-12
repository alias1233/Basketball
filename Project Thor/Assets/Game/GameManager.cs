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
            int RedTeamNum = 0;
            int BlueTeamNum = 0;

            foreach(PlayerInformation player in PlayerList)
            {
                if(player.Team == Teams.Red)
                {
                    RedTeamNum++;
                }

                if(player.Team == Teams.Blue)
                {
                    BlueTeamNum++;
                }
            }

            Teams team = Teams.Red;

            if (BlueTeamNum < RedTeamNum)
            {
                team = Teams.Blue;
            }

            PlayerList.Add(new PlayerInformation(clientId, team));

            GameObject newPlayer = Instantiate(PlayerPrefab, GetSpawnLocation(team), Quaternion.identity);

            newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

            return;
        }

        for(int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].Id == clientId)
            {
                PlayerList.RemoveAt(i);

                return;
            }
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