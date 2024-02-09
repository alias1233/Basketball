using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 3f;

    public int ServerDelay = 2;

    private Dictionary<int, Inputs> InputsDictionary = new Dictionary<int, Inputs>();
    private Inputs CurrentInput;
    private int TimeStamp;
    private Vector3 MoveDirection;

    bool AutonomousProxy;

    // Start is called before the first frame update
    void Start()
    {
        AutonomousProxy = IsClient && IsOwner;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        TimeStamp++;

        if (AutonomousProxy)
        {
            AutonomousProxyTick();
        }

        if (IsServer && !IsOwner)
        {
            ServerTickForOtherPlayers();
        }

        if (IsServer)
        {
            ServerTickForAll();
        }
    }

    void ServerTickForAll()
    {
        ReplicatePositionClientRpc(transform.position);
    }

    void ServerTickForOtherPlayers()
    {
        transform.position += MoveDirection * moveSpeed * Time.deltaTime;
    }

    void AutonomousProxyTick()
    {
        CurrentInput = new Inputs(TimeStamp, Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.A), Input.GetKey(KeyCode.S), Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.Space));

        InputsDictionary.Add(TimeStamp, CurrentInput);

        SendInputsServerRpc(CurrentInput);

        if (CurrentInput.W)
        {
            MoveDirection.z = 1f;
        }

        if (CurrentInput.A)
        {
            MoveDirection.x = -1f;
        }

        if (CurrentInput.S)
        {
            MoveDirection.z = -1f;
        }

        if (CurrentInput.D)
        {
            MoveDirection.x = 1f;
        }

        transform.position += MoveDirection * moveSpeed * Time.deltaTime;
    }

    [ClientRpc]
    public void ReplicatePositionClientRpc(Vector3 position)
    {
        transform.position = position;
    }

    [ServerRpc]
    public void SendInputsServerRpc(Inputs input)
    {
        InputsDictionary.Add(input.TimeStamp, input);
    }
}
