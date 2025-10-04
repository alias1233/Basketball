using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;
using static ConnectionNotificationManager;
using TMPro;

public enum Teams
{ 
    Red,
    Blue,
}

public enum Characters:int
{ 
    V1,
    BigBertha
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Singleton { get; internal set; }

    [SerializeField]
    private GameObject PlayerPrefab;

    public GameObject[] CharacterList;

    [SerializeField]
    private Transform RedTeamSpawn;
    [SerializeField]
    private Transform BlueTeamSpawn;

    [SerializeField]
    private Transform Graveyard;

    private NetworkList<PlayerInformation> PlayerList;

    private List<PlayerObject> RedTeamPlayers = new List<PlayerObject>();
    private List<PlayerObject> BlueTeamPlayers = new List<PlayerObject>();

    private bool bStartGame;
    private bool bOvertime;

    public Hoop RedTeamHoop;
    public Hoop BlueTeamHoop;

    private NetworkVariable<int> RedTeamScore = new NetworkVariable<int>();
    private NetworkVariable<int> BlueTeamScore = new NetworkVariable<int>();

    public TMP_Text RedTeamScoreText;
    public TMP_Text BlueTeamScoreText;

    public Transform RedTeamHoopTransform;
    public Transform BlueTeamHoopTransform;

    public float LaunchFactor = 1000;

    public TMP_Text MatchTimer;

    public float MatchTime;
    public int UpdateMatchTimeInterval;

    private int UpdateMatchTimeTick;

    private float DeltaTime;

    public GameObject EndGameObject;
    public TMP_Text EndGameText;

    public GameObject Lebron;

    public GameObject Tip;

    private void Awake()
    {
        Singleton = this;

        PlayerList = new NetworkList<PlayerInformation>();

        DeltaTime = Time.fixedDeltaTime;
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

    public void StartGame()
    {
        bStartGame = true;

        Invoke(nameof(DisableTip), 10);
    }

    private void DisableTip()
    {
        Tip.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!bStartGame)
        {
            return;
        }

        MatchTime -= DeltaTime;

        UpdateTimer(MatchTime);

        if (!IsServer)
        {
            return;
        }

        if (MatchTime < 0)
        {
            EndGame();
        }

        UpdateMatchTimeTick++;

        if (UpdateMatchTimeTick >= UpdateMatchTimeInterval)
        {
            UpdateMatchTimeTick = 0;

            UpdateMatchTimeClientRpc(MatchTime);
        }
    }

    private void EndGame()
    {
        bStartGame = false;

        if (RedTeamScore.Value == BlueTeamScore.Value)
        {
            StartOvertimeClientRpc();

            return;
        }

        Teams WinningTeam;

        if(RedTeamScore.Value > BlueTeamScore.Value)
        {
            WinningTeam = Teams.Red;
        }

        else
        {
            WinningTeam = Teams.Blue;
        }

        EndGameClientRpc(WinningTeam);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void EndGameClientRpc(Teams team)
    {
        if (team == Teams.Red)
        {
            EndGameText.text = "RED TEAM WINS!";
        }

        else
        {
            EndGameText.text = "BLUE TEAM WINS!";
        }

        ulong ID = NetworkManager.LocalClientId;
        List<PlayerInformation> PlayerList = GetAllPlayerInformation();

        foreach (PlayerInformation i in PlayerList)
        {
            if(i.Id == ID)
            {
                if(i.Team == team)
                {

                }

                else
                {

                }

                break;
            }
        }

        EndGameObject.SetActive(true);

        Lebron.SetActive(true);
    }

    [ClientRpc(Delivery = RpcDelivery.Reliable)]
    private void StartOvertimeClientRpc()
    {
        MatchTimer.text = "OVERTIME";

        bOvertime = true;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    private void UpdateMatchTimeClientRpc(float CurrentMatchTime)
    {
        if(IsServer || bOvertime)
        {
            return;
        }

        MatchTime = CurrentMatchTime;

        UpdateTimer(MatchTime);
    }

    private void UpdateTimer(float CurrentMatchTime)
    {
        int seconds = (int)(CurrentMatchTime % 60);

        if (seconds < 10)
        {
            MatchTimer.text = (int)(CurrentMatchTime / 60) + " : 0" + seconds;

            return;
        }

        MatchTimer.text = (int)(CurrentMatchTime / 60) + " : " + (int)(CurrentMatchTime % 60);
    }

    public void ScorePoint(Teams team)
    {
        Ball.Singleton.Velocity = Vector3.zero;

        if(team == Teams.Red)
        {
            BlueTeamScore.Value++;

            if(bOvertime)
            {
                EndGameClientRpc(Teams.Blue);
            }

            return;
        }

        if(team == Teams.Blue)
        {
            RedTeamScore.Value++;

            if (bOvertime)
            {
                EndGameClientRpc(Teams.Red);
            }
        }
    }

    private void UpdateRedTeamScore(int previous, int current)
    {
        RedTeamScoreText.text = current.ToString();
        BlueTeamHoop.OnScore();

        if(IsServer)
        {
            foreach (PlayerObject i in RedTeamPlayers)
            {
                BasePlayerManager launchedplayer = i.GetPlayerCharacter();

                if (launchedplayer)
                {
                    float LaunchMagnitude = 1 / Mathf.Clamp((launchedplayer.transform.position - BlueTeamHoopTransform.position).magnitude, 1, 100);
                    Vector3 LaunchDirection = launchedplayer.transform.position - BlueTeamHoopTransform.position;
                    Vector3 LaunchDirectionNormalized = new Vector3(LaunchDirection.x, 0, LaunchDirection.z).normalized;

                    launchedplayer.OnScore(
                    LaunchFactor * new Vector3(LaunchDirectionNormalized.x, 0.5f, LaunchDirectionNormalized.z) * LaunchMagnitude
                    );
                }
            }
        }
    }

    private void UpdateBlueTeamScore(int previous, int current)
    {
        BlueTeamScoreText.text = current.ToString();
        RedTeamHoop.OnScore();

        if(IsServer)
        {
            foreach (PlayerObject i in BlueTeamPlayers)
            {
                BasePlayerManager launchedplayer = i.GetPlayerCharacter();

                if (launchedplayer)
                {
                    float LaunchMagnitude = 1 / Mathf.Clamp((launchedplayer.transform.position - RedTeamHoopTransform.position).magnitude, 1, 100);
                    Vector3 LaunchDirection = launchedplayer.transform.position - RedTeamHoopTransform.position;
                    Vector3 LaunchDirectionNormalized = new Vector3(LaunchDirection.x, 0, LaunchDirection.z).normalized;

                    launchedplayer.OnScore(
                    LaunchFactor * new Vector3(LaunchDirectionNormalized.x, 0.5f, LaunchDirectionNormalized.z) * LaunchMagnitude
                    );
                }
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

            GameObject newPlayer = Instantiate(PlayerPrefab);

            newPlayer.GetComponent<NetworkObject>().SpawnWithOwnership(clientId);
            PlayerObject playerobject = newPlayer.GetComponent<PlayerObject>();

            if (team == Teams.Red)
            {
                RedTeamPlayers.Add(playerobject);
                playerobject.Team = Teams.Red;
            }

            else
            {
                BlueTeamPlayers.Add(playerobject);
                playerobject.Team = Teams.Blue;
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