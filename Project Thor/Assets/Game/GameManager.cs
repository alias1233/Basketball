using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using TMPro;
using static ConnectionNotificationManager;
using Unity.VisualScripting;

public class GameManager : NetworkBehaviour
{
    public static GameManager Singleton { get; internal set; }

    private NetworkList<PlayerInformation> PlayerList;

    public TMP_Text TESTINGTEXT;

    private void Awake()
    {
        Singleton = this;

        PlayerList = new NetworkList<PlayerInformation>();
    }

    private void Start()
    {
        ConnectionNotificationManager.Singleton.OnClientConnectionNotification += UpdatePlayerList;
    }

    private void Update()
    {
        
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient)
        {
            PlayerList.OnListChanged += OnClientPlayerListChanged;
        }

        if (IsServer)
        {
            PlayerList.OnListChanged += OnServerPlayerListChanged;
        }
    }

    void OnServerPlayerListChanged(NetworkListEvent<PlayerInformation> changeEvent)
    {

    }

    void OnClientPlayerListChanged(NetworkListEvent<PlayerInformation> changeEvent)
    {

    }

    private void UpdatePlayerList(ulong clientid, ConnectionStatus connection)
    {
        if(!IsServer)
        {
            return;
        }

        ushort team = (ushort)(clientid % 2);

        PlayerInformation playerinfo = new PlayerInformation(clientid, team);

        if (connection == ConnectionStatus.Connected)
        {
            PlayerList.Add(playerinfo);
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

    public override void OnDestroy()
    {
        base.OnDestroy();
    }
}