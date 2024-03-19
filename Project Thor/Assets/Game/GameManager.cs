using System;
using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using static ConnectionNotificationManager;
using TMPro;
using static UnityEngine.Rendering.DebugUI;

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

    private List<PlayerManager> RedTeamPlayers = new List<PlayerManager>();
    private List<PlayerManager> BlueTeamPlayers = new List<PlayerManager>();

    public Hoop RedTeamHoop;
    public Hoop BlueTeamHoop;

    private NetworkVariable<int> RedTeamScore = new NetworkVariable<int>();
    private NetworkVariable<int> BlueTeamScore = new NetworkVariable<int>();

    public TMP_Text RedTeamScoreText;
    public TMP_Text BlueTeamScoreText;

    public Transform RedTeamHoopTransform;
    public Transform BlueTeamHoopTransform;

    public float LaunchFactor = 1000;

    private void Awake()
    {
        Singleton = this;

        PlayerList = new NetworkList<PlayerInformation>();
    }

    private void Start()
    {
        ConnectionNotificationManager.Singleton.OnClientConnectionNotification += UpdatePlayers;
    }

    public override void OnNetworkSpawn()
    {
        RedTeamScore.OnValueChanged += UpdateRedTeamScore;
        BlueTeamScore.OnValueChanged += UpdateBlueTeamScore;
    }

    public override void OnNetworkDespawn()
    {
        RedTeamScore.OnValueChanged -= UpdateRedTeamScore;
        BlueTeamScore.OnValueChanged -= UpdateBlueTeamScore;
    }

    public void ScorePoint(Teams team)
    {
        if(team == Teams.Red)
        {
            BlueTeamScore.Value++;

            return;
        }

        if(team == Teams.Blue)
        {
            RedTeamScore.Value++;
        }
    }

    private void UpdateRedTeamScore(int previous, int current)
    {
        RedTeamScoreText.text = current.ToString();
        BlueTeamHoop.OnScore();

        if(IsServer)
        {
            foreach (PlayerManager i in RedTeamPlayers)
            {
                i.OnScore(
                    LaunchFactor * 1 / Mathf.Clamp((i.transform.position - BlueTeamHoopTransform.position).magnitude, 1, 100) * (i.transform.position - BlueTeamHoopTransform.position).normalized
                    );
            }
        }
    }

    private void UpdateBlueTeamScore(int previous, int current)
    {
        BlueTeamScoreText.text = current.ToString();
        RedTeamHoop.OnScore();

        if(IsServer)
        {
            foreach (PlayerManager i in BlueTeamPlayers)
            {
                i.OnScore(
                    LaunchFactor * 1 / Mathf.Clamp((i.transform.position - RedTeamHoopTransform.position).magnitude, 1, 100) * (i.transform.position - RedTeamHoopTransform.position).normalized
                    );
            }
        }
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

            if (team == Teams.Red)
            {
                RedTeamPlayers.Add(newPlayer.GetComponent<PlayerManager>());
            }

            else
            {
                BlueTeamPlayers.Add(newPlayer.GetComponent<PlayerManager>());
            }

            return;
        }

        for(int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].Id == clientId)
            {
                PlayerList.RemoveAt(i);
            }
        }

        for (int i = 0; i < RedTeamPlayers.Count; i++)
        {
            if (RedTeamPlayers[i].OwnerClientId == clientId)
            {
                RedTeamPlayers.RemoveAt(i);
            }
        }

        for (int i = 0; i < BlueTeamPlayers.Count; i++)
        {
            if (BlueTeamPlayers[i].OwnerClientId == clientId)
            {
                BlueTeamPlayers.RemoveAt(i);
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