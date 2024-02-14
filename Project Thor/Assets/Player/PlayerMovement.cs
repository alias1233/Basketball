using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Components")]

    public Transform Orientation;
    public Camera PlayerCamera;
    private CapsuleCollider Collider;

    [Header("Ticking")]

    public int FixedTickPerSecond = 60;
    public int TickTime;
    public int ServerDelay = 10;
    public int ServerMaxFixedTicksPerFrame = 30;
    public int MaxFixedTicksPerFrame = 60;

    private int TimeStamp;
    private float DeltaTime;

    [Header("Networking")]

    private ClientRpcParams OwningClientID;
    private bool bAutonomousProxy;
    private bool bSimulatedProxy;

    [Header("Client Data")]

    private Dictionary<int, Inputs> InputsDictionary = new Dictionary<int, Inputs>();
    private Inputs CurrentInput;

    [Header("Replicate Movement")]

    public float ReplicatePositionInterval = 0.1f;

    private float LastTimeReplicatedPosition;

    [Header("Client Corrections")]

    public float CorrectionDistance = 0.5f;
    public float MinTimeBetweenCorrections = 1;
    public int DefaultCorrectionSmoothTime;
    public float SmallCorrectionThreshold;
    public int SmallCorrectionSmoothTime;

    private Dictionary<int, Vector3> ClientDataDictionary = new Dictionary<int, Vector3>();
    private ClientCorrection ServerState;
    private float LastTimeSentCorrection;
    private float LastTimeSentClientData;
    private bool ReplayMoves;
    private int SimulateTimeStamp;
    private bool bSmoothingCorrection;
    private bool bSmoothingPosition;
    private int StartSmoothingCorrectionTime;
    private Vector3 StartCorrectionPosition;
    private Vector3 CorrectedPosition;
    private int CorrectionSmoothTime;

    private int TotalTimes;
    private int TotalTimeDifference;
    private int LateInputsCount;
    private int EarlyInputsCount;
    private float LastTimeSentClientTimeCorrection;

    [Header("Movement")]

    public LayerMask layerMask;
    //public float MaxSlopeAngle = 55;

    private Bounds bounds;
    private int MaxBounces = 5;
    private float SkinWidth = 0.015f;

    public float WalkMoveSpeed = 3f;
    public float SprintMoveSpeed = 6f;
    public float JumpForce = 10f;
    public LayerMask WhatIsGround;
    public int JumpCooldown = 30;
    public float GroundFriction = 8;
    public float AirFriction = 0.25f;
    public float Gravity = 1;

    private Quaternion Rotation;
    private Quaternion ForwardRotation;
    private Vector3 MoveDirection;
    private bool bIsGrounded;

    /*
     * 
     * Variables That Need To Be Sent For Client Corrections
     * 
     */

    private Vector3 Velocity;
    private int LastTimeJumped;

    //TESTING
    //private int Ticking;
    //private bool bTick;
    //private float FixedTickRate;
    //private float FixedTickDelta;

    // Start is called before the first frame update
    void Start()
    {
        Collider = GetComponent<CapsuleCollider>();

        DeltaTime = Time.fixedDeltaTime;

        bAutonomousProxy = IsClient && IsOwner && !IsServer;

        bSimulatedProxy = IsClient && !IsOwner && !IsServer;

        if(IsServer && !IsOwner)
        {
            TimeStamp = -ServerDelay;

            OwningClientID = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            };
        }

        //FixedTickRate = 1.0f / FixedTickPerSecond;
    }

    void FixedUpdate()
    {
        TimeStamp++;

        if (IsServer)
        {
            if (IsOwner)
            {
                HostTick();
            }

            else
            {
                ServerTickForOtherPlayers();
            }

            ServerTickForAll();
        }

        if (bAutonomousProxy)
        {
            AutonomousProxyTick();

            return;
        }

        if(bSimulatedProxy)
        {
            SimulatedProxyTick();

            return;
        }
    }

    void HostTick()
    {
        Rotation = PlayerCamera.transform.rotation;

        CurrentInput = CreateInput();

        HandleInputs(CurrentInput);

        MovePlayer();
    }

    void ServerTickForOtherPlayers()
    {
        if (InputsDictionary.TryGetValue(TimeStamp, out Inputs inputs))
        {
            CurrentInput = inputs;
        }

        HandleInputs(CurrentInput);

        MovePlayer();

        if (ClientDataDictionary.TryGetValue(TimeStamp, out Vector3 clientposition))
        {
            CheckClientPositionError(transform.position, clientposition);
        }
    }

    void AutonomousProxyTick()
    {
        if (bSmoothingPosition)
        {
            bSmoothingPosition = false;

            transform.position = CorrectedPosition;

            if (TimeStamp - StartSmoothingCorrectionTime >= CorrectionSmoothTime)
            {
                bSmoothingCorrection = false;
            }
        }

        if (ReplayMoves)
        {
            ReplayMovesAfterCorrection();
        }

        Rotation = PlayerCamera.transform.rotation;

        CurrentInput = CreateInput();

        InputsDictionary.Add(TimeStamp, CurrentInput);

        SendInputsServerRpc(CurrentInput);

        HandleInputs(CurrentInput);

        MovePlayer();

        if (Time.time - LastTimeSentClientData > 0.33)
        {
            LastTimeSentClientData = Time.time;

            SendClientDataServerRpc(TimeStamp, transform.position);
        }

        if (bSmoothingCorrection)
        {
            bSmoothingPosition = true;

            CorrectedPosition = transform.position;

            float Alpha = (float)(TimeStamp - StartSmoothingCorrectionTime) / CorrectionSmoothTime;

            Vector3 SmoothPosition = Vector3.Lerp(StartCorrectionPosition, CorrectedPosition, Alpha);

            transform.position = SmoothPosition;
        }
    }

    void ServerTickForAll()
    {
        if (Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
        {
            LastTimeReplicatedPosition = Time.time;
            
            ReplicatePositionClientRpc(transform.position, Velocity, Rotation);
        }
    }

    void SimulatedProxyTick()
    {
        MovePlayer();
    }

    void MovePlayer()
    {
        bIsGrounded = Physics.Raycast(transform.position, Vector3.down, Collider.height * 0.5f + 0.15f, WhatIsGround);

        float MoveSpeed = WalkMoveSpeed;

        if (CurrentInput.Shift)
        {
            MoveSpeed = SprintMoveSpeed;
        }

        Vector3 JumpVel = Vector3.zero;
        Vector3 GravityVel = Gravity * Vector3.down;
        float frictionmultiplier = 1 - AirFriction * DeltaTime;
        Vector3 InputVel = (MoveDirection * MoveSpeed) * (1 + AirFriction) * DeltaTime;

        if (bIsGrounded)
        {
            GravityVel = Vector3.zero;

            frictionmultiplier = 1 - GroundFriction * DeltaTime;

            InputVel = (MoveDirection * MoveSpeed) * (1 + GroundFriction) * DeltaTime;

            Velocity.y = 0;

            if (CurrentInput.SpaceBar && TimeStamp - LastTimeJumped > JumpCooldown)
            {
                LastTimeJumped = TimeStamp;

                JumpVel = Vector3.up * JumpForce;
            }
        }

        Vector3 PreviousLocation = transform.position;

        Vector3 Delta = (Velocity * frictionmultiplier + InputVel + JumpVel + GravityVel) * DeltaTime;

        bounds = Collider.bounds;
        bounds.Expand(-2 * SkinWidth);

        transform.Translate(CollideAndSlide(transform.position, Delta, 0, Delta));

        Velocity = (transform.position - PreviousLocation) / DeltaTime;
    }

    Vector3 CollideAndSlide(Vector3 Pos, Vector3 Vel, int depth, Vector3 VelInit)
    {
        if(depth >= MaxBounces)
        {
            return Vector3.zero;
        }

        float dist = Vel.magnitude + SkinWidth;

        Vector3 p1 = transform.position + Collider.center + Vector3.up * -Collider.height * 0.25F;
        Vector3 p2 = p1 + Vector3.up * Collider.height * 0.5f;
        RaycastHit hit;

        if (Physics.CapsuleCast(
            p1,
            p2,
            bounds.extents.x,
            Vel.normalized,
            out hit,
            dist,
            layerMask
            ))
        {
            Vector3 SnapToSurface = Vel.normalized * (hit.distance - SkinWidth);
            Vector3 Leftover = Vel - SnapToSurface;

            if(SnapToSurface.magnitude <= SkinWidth)
            {
                SnapToSurface = Vector3.zero;
            }

            /*
            float Angle = Vector3.Angle(Vector3.up, hit.normal);

            if(Angle <= MaxSlopeAngle)
            {
                Leftover = ProjectAndScale(Leftover, hit.normal);
            }

            else
            {
                float Scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(VelInit.x, 0, VelInit.z).normalized
                    );

                if(bIsGrounded)
                {
                    Leftover = ProjectAndScale(
                        new Vector3(Leftover.x, 0, Leftover.z),
                        new Vector3(hit.normal.x, 0, hit.normal.z)
                        ).normalized;

                    Leftover *= Scale;
                }

                else
                {
                    Leftover = ProjectAndScale(Leftover, hit.normal) * Scale;
                }
            }
            */

            Leftover = ProjectAndScale(Leftover, hit.normal);

            return SnapToSurface + CollideAndSlide(Pos + SnapToSurface, Leftover, depth + 1, VelInit);
        }

        return Vel;
    }

    private Vector3 ProjectAndScale(Vector3 Vec, Vector3 Normal)
    {
        float Mag = Vec.magnitude;
        Vec = Vector3.ProjectOnPlane(Vec, Normal).normalized;
        Vec *= Mag;

        return Vec;
    }

/*
*
* Client Corrections
*
*/

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void SendClientDataServerRpc(int timestamp, Vector3 position)
    {
        ClientDataDictionary.Add(timestamp, position);

        CheckClientTimeError(timestamp);
    }

    public void CheckClientPositionError(Vector3 serverpos, Vector3 clientpos)
    {
        if(Time.time - LastTimeSentCorrection < MinTimeBetweenCorrections)
        {
            return;
        }

        float distancebetweenclientserver = (clientpos - serverpos).magnitude;

        if (distancebetweenclientserver <= CorrectionDistance)
        {
            transform.position = clientpos;
        }

        else
        {
            LastTimeSentCorrection = Time.time;

            SendClientCorrection();

            print("Client Correction Sent!");
        }
    }

    public void SendClientCorrection()
    {
        ClientCorrection Data = new ClientCorrection(TimeStamp, transform.position, Velocity, LastTimeJumped);

        ClientCorrectionClientRpc(Data, OwningClientID);
    }

    void SetToServerState()
    {
        transform.position = ServerState.Position;
        Velocity = ServerState.Velocity;
        LastTimeJumped = ServerState.LastTimeJumped;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ClientCorrectionClientRpc(ClientCorrection Data, ClientRpcParams clientRpcParams = default)
    {
        AfterCorrectionReceived(Data.TimeStamp);

        ServerState = Data;
    }

    void AfterCorrectionReceived(int replaytimestamp)
    {
        ReplayMoves = true;
        SimulateTimeStamp = replaytimestamp;

        bSmoothingCorrection = true;
        StartSmoothingCorrectionTime = TimeStamp;
        StartCorrectionPosition = transform.position;
    }

    void ReplayMovesAfterCorrection()
    {
        SetToServerState();

        int currentTime = TimeStamp;
        TimeStamp = SimulateTimeStamp + 1;

        while (TimeStamp < currentTime)
        {
            if (InputsDictionary.TryGetValue(TimeStamp, out Inputs inputs))
            {
                CurrentInput = inputs;
            }

            HandleInputs(CurrentInput);

            MovePlayer();

            TimeStamp++;
        }

        ReplayMoves = false;

        if ((transform.position - StartCorrectionPosition).magnitude < SmallCorrectionThreshold)
        {
            CorrectionSmoothTime = SmallCorrectionSmoothTime;

            return;
        }

        CorrectionSmoothTime = DefaultCorrectionSmoothTime;
    }

    void CheckClientTimeError(int clienttime)
    {
        if (Time.time - LastTimeSentClientTimeCorrection < 1)
        {
            return;
        }

        TotalTimes++;

        int CorrectTime = TimeStamp + ServerDelay;

        TotalTimeDifference = TotalTimeDifference + CorrectTime - clienttime;

        if (clienttime < TimeStamp)
        {
            //LateInputsCount++;
        }

        if (clienttime > TimeStamp + 2 * ServerDelay)
        {
            //EarlyInputsCount++;
        }

        if (LateInputsCount >= 3 || EarlyInputsCount >= 3 || Time.time - LastTimeSentClientTimeCorrection > 5)
        {
            LateInputsCount = 0;
            EarlyInputsCount = 0;
            LastTimeSentClientTimeCorrection = Time.time;

            int timediff = TotalTimeDifference / TotalTimes;

            if (timediff != 0)
            {
                SendClientTimeCorrectionClientRpc(timediff, OwningClientID);

                print(TotalTimeDifference / TotalTimes);
            }

            TotalTimeDifference = 0;
            TotalTimes = 0;
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void SendClientTimeCorrectionClientRpc(int timediff, ClientRpcParams clientRpcParams = default)
    {
        TimeStamp += timediff;
    }

/*
*
* Inputs
*
*/

    Inputs CreateInput()
    {
        Inputs input = new Inputs(TimeStamp, Rotation, Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.A), Input.GetKey(KeyCode.S), Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.Space), Input.GetKey(KeyCode.LeftShift));

        return input;
    }

    void HandleInputs(Inputs input)
    {
        MoveDirection = Vector3.zero;

        Quaternion quaternion = input.Rotation;

        float a = Mathf.Sqrt((quaternion.w * quaternion.w) + (quaternion.y * quaternion.y));
        ForwardRotation = new Quaternion(x: 0, y: quaternion.y, z: 0, w: quaternion.w / a);

        Orientation.rotation = ForwardRotation;

        if (input.W)
        {
            MoveDirection += Orientation.transform.forward;
        }

        if (input.A)
        {
            MoveDirection -= Orientation.transform.right;
        }

        if (input.S)
        {
            MoveDirection -= Orientation.transform.forward;
        }

        if (input.D)
        {
            MoveDirection += Orientation.transform.right;
        }

        MoveDirection.Normalize();
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void SendInputsServerRpc(Inputs input)
    {
        InputsDictionary.Add(input.TimeStamp, input);

        CheckClientTimeError(input.TimeStamp);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicatePositionClientRpc(Vector3 position, Vector3 velocity, Quaternion rotation)
    {
        if (IsOwner || IsServer)
        {
            return;
        }

        transform.position = position;
        Velocity = velocity;

        Quaternion quaternion = rotation;

        float a = Mathf.Sqrt((quaternion.w * quaternion.w) + (quaternion.y * quaternion.y));
        ForwardRotation = new Quaternion(x: 0, y: quaternion.y, z: 0, w: quaternion.w / a);

        Orientation.transform.rotation = ForwardRotation;
    }

    public Vector3 GetVelocity()
    {
        return Velocity;
    }

    public bool GetIsGrounded()
    {
        return bIsGrounded;
    }
}
