using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using static ConnectionNotificationManager;

public class PlayerMovement : NetworkBehaviour
{
    [Header("Components")]

    private PlayerManager Player;
    public Transform Orientation;
    public Transform FPTransform;
    private CapsuleCollider Collider;

    [Header("Ticking")]

    private int CurrentTimeStamp;
    private float DeltaTime;

    [Header("Networking")]

    private ClientRpcParams OwningClientID;
    private ClientRpcParams IgnoreOwnerRPCParams;
    private List<ulong> ClientIDList = new List<ulong>();
    private NetworkRole LocalRole;

    [Header("Client Data")]

    private Dictionary<int, Inputs> InputsDictionary = new Dictionary<int, Inputs>();
    private Inputs CurrentInput;

    [Header("Replicate Movement")]

    public float ReplicatePositionInterval = 0.1f;

    private float LastTimeReplicatedPosition;
    private bool bUpdatedThisFrame;

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

    [Header("// Movement //")]

    public LayerMask layerMask;
    public int MaxBounces = 5;
    public float SkinWidth = 0.015f;

    private Bounds bounds;
    private Vector3 ColliderOffset1;
    private Vector3 ColliderOffset2;

    public float WalkMoveSpeed = 3f;
    public float SlideMoveSpeed = 6f;
    public float JumpForce = 10f;
    public LayerMask WhatIsGround;
    public int JumpCooldown = 30;
    public float GroundFriction = 8;
    public float SlideFriction = 0.1f;
    public float AirFriction = 0.25f;
    public float Gravity = 1;

    private Quaternion Rotation;
    private Quaternion ForwardRotation;
    private Vector3 MoveDirection;
    private bool bIsGrounded;
    private bool bIsSliding;

    private bool bChangingVelocity;

    [Header("Visuals")]

    public Vector3 SlideCameraOffset;
    public float SlideCameraDuration;

    public ParticleSystem SlideSmoke;

    /*
    * 
    * Variables That Need To Be Sent For Client Corrections
    * 
    */

    private bool bNoMovement;
    private Vector3 Velocity;
    private int LastTimeJumped;

    [Header("// Abilities //")]

    public int DashDuration;
    public int DashCooldown;
    public float DashSpeed;
    public float AfterDashVelocityMagnitude;

    [Header("Visuals")]

    public float DashFOVOffset;
    public float DashFOVDuration;

    public ParticleSystem FirstPersonDashParticles;
    public ParticleSystem ThirdPersonDashParticles;
    public TrailRenderer DashTrails;

    /*
    * 
    * Variables That Need To Be Sent For Client Corrections
    * 
    */

    private bool bDashing;
    private int StartDashTime;
    private Quaternion DashingStartRotation;

    [Header("TESTING")]

    public bool ALLOWTESTING;
    public bool TESTINGBOOL;
    public bool TESTINGBOOL2;
    public int TESTINGINT;

    /*
    public float MaxSlopeAngle = 55;
    */

    // Start is called before the first frame update
    void Start()
    {
        Player = GetComponent<PlayerManager>();
        Collider = GetComponent<CapsuleCollider>();

        bounds = Collider.bounds;
        bounds.Expand(-2 * SkinWidth);
        ColliderOffset1 = Collider.center + Vector3.up * -Collider.height * 0.25f;
        ColliderOffset2 = Vector3.up * Collider.height * 0.5f;

        DeltaTime = Time.fixedDeltaTime;

        LocalRole = Player.GetLocalRole();

        if(IsServer)
        {
            OwningClientID = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            };

            foreach (ulong i in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (i != OwnerClientId && i != 0)
                {
                    ClientIDList.Add(i);
                }
            }

            IgnoreOwnerRPCParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = ClientIDList
                }
            };
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ConnectionNotificationManager.Singleton.OnClientConnectionNotification += UpdateClientSendRPCParams;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            ConnectionNotificationManager.Singleton.OnClientConnectionNotification -= UpdateClientSendRPCParams;
        }
    }

    private void UpdateClientSendRPCParams(ulong clientId, ConnectionStatus connection)
    {
        if (connection == ConnectionStatus.Connected)
        {
            ClientIDList.Add(clientId);
        }

        else
        {
            ClientIDList.Remove(clientId);
        }

        IgnoreOwnerRPCParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = ClientIDList
            }
        };
    }

    public void FixedTick(int timestamp)
    {
        CurrentTimeStamp = timestamp;

        switch (LocalRole)
        {
            case NetworkRole.HostOwner:

                HostTick();

                ServerTickForAll();

                break;

            case NetworkRole.HostProxy:

                ServerTickForOtherPlayers();

                ServerTickForAll();

                break;

            case NetworkRole.AutonomousProxy:

                AutonomousProxyTick();

                break;

            case NetworkRole.SimulatedProxy:

                SimulatedProxyTick();

                break;
        }
    }

    void HostTick()
    {
        /*
        if(ALLOWTESTING)
        {
            Rotation = FPTransform.rotation;

            if (Input.GetKeyDown(KeyCode.CapsLock))
            {
                TESTINGBOOL = true;
            }

            if (TESTINGBOOL)
            {
                if (CurrentTimeStamp - TESTINGINT >= 120)
                {
                    TESTINGINT = CurrentTimeStamp;

                    TESTINGBOOL2 = !TESTINGBOOL2;
                }

                if (TESTINGBOOL2)
                {
                    CurrentInput = new Inputs(CurrentTimeStamp, Rotation, false, true, false, false, false, true, false);
                }

                else
                {
                    CurrentInput = new Inputs(CurrentTimeStamp, Rotation, false, false, false, true, false, true, false);
                }
            }

            else
            {
                CurrentInput = CreateInput();
            }

            HandleInputs(CurrentInput);

            MovePlayer();

            return;
        }
        */

        Rotation = FPTransform.transform.rotation;

        CreateInputs(ref CurrentInput);

        HandleInputs(ref CurrentInput);

        MovePlayer();
    }

    void ServerTickForOtherPlayers()
    {
        if (InputsDictionary.TryGetValue(CurrentTimeStamp, out Inputs inputs))
        {
            InputsDictionary.Remove(CurrentTimeStamp);

            CurrentInput = inputs;
        }

        HandleInputs(ref CurrentInput);

        FPTransform.transform.rotation = Rotation;

        MovePlayer();

        if (ClientDataDictionary.TryGetValue(CurrentTimeStamp, out Vector3 clientposition))
        {
            ClientDataDictionary.Remove(CurrentTimeStamp);

            CheckClientPositionError(transform.position, clientposition);
        }
    }

    void AutonomousProxyTick()
    {
        if (bSmoothingPosition)
        {
            bSmoothingPosition = false;

            transform.position = CorrectedPosition;

            if (CurrentTimeStamp - StartSmoothingCorrectionTime >= CorrectionSmoothTime)
            {
                bSmoothingCorrection = false;
            }
        }

        if (ReplayMoves)
        {
            ReplayMovesAfterCorrection();
        }

        Rotation = FPTransform.rotation;

        CreateInputs(ref CurrentInput);

        InputsDictionary[CurrentTimeStamp] = CurrentInput;

        SendInputsServerRpc(CurrentInput);

        HandleInputs(ref CurrentInput);

        MovePlayer();

        if (Time.time - LastTimeSentClientData > 0.33)
        {
            LastTimeSentClientData = Time.time;

            SendClientDataServerRpc(CurrentTimeStamp, transform.position);
        }

        if (bSmoothingCorrection)
        {
            bSmoothingPosition = true;
            CorrectedPosition = transform.position;

            transform.position = Vector3.Lerp(StartCorrectionPosition, CorrectedPosition, (float)(CurrentTimeStamp - StartSmoothingCorrectionTime) / CorrectionSmoothTime);
        }
    }

    void ServerTickForAll()
    {
        if(StartDashTime == CurrentTimeStamp)
        {
            ReplicateDashingClientRpc(IgnoreOwnerRPCParams);
        }

        if (Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
        {
            LastTimeReplicatedPosition = Time.time;
            
            ReplicatePositionClientRpc(transform.position, Velocity, Rotation, bIsSliding, IgnoreOwnerRPCParams);
        }
    }

    void SimulatedProxyTick()
    {
        if(ThirdPersonDashParticles.isPlaying)
        {
            ThirdPersonDashParticles.transform.position = FPTransform.position + Velocity.normalized * 2;
            ThirdPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPTransform.position - ThirdPersonDashParticles.transform.position), Vector3.up);
        }

        else
        {
            DashTrails.emitting = false;
        }

        if(bUpdatedThisFrame)
        {
            bUpdatedThisFrame = false;

            return;
        }

        SafeMovePlayer(Velocity * DeltaTime);
    }

    void AbilityTick()
    {
        if(CurrentInput.Shift && CurrentTimeStamp - StartDashTime >= DashCooldown)
        {
            StartDashTime = CurrentTimeStamp;
            bDashing = true;
            DashingStartRotation = Rotation;

            bNoMovement = true;

            if(IsOwner)
            {
                FirstPersonDashParticles.transform.position = FPTransform.transform.position + DashingStartRotation * Vector3.forward * 2;
                FirstPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPTransform.transform.position - ThirdPersonDashParticles.transform.position), Vector3.up);

                FirstPersonDashParticles.Play();
                DashTrails.emitting = true;

                Player.ChangeFOV(DashFOVOffset, DashFOVDuration);
            }

            else
            {
                ThirdPersonDashParticles.transform.position = FPTransform.position + DashingStartRotation * Vector3.forward * 2;
                ThirdPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPTransform.position - ThirdPersonDashParticles.transform.position), Vector3.up);

                ThirdPersonDashParticles.Play();
                DashTrails.emitting = true;
            }
        }

        if(bDashing)
        {
            if (CurrentTimeStamp - StartDashTime <= DashDuration)
            {
                SafeMovePlayer(DashingStartRotation * Vector3.forward * DashSpeed * DeltaTime);
            }

            else
            {
                bDashing = false;
                bNoMovement = false;

                Velocity *= AfterDashVelocityMagnitude;
                bChangingVelocity = true;

                if (IsOwner)
                {
                    FirstPersonDashParticles.Stop();
                    DashTrails.emitting = false;
                }

                else
                {
                    DashTrails.emitting = false;
                }
            }
        }
    }

    void MovePlayer()
    {
        Vector3 PreviousLocation = transform.position;

        AbilityTick();

        if(bNoMovement)
        {
            if(bChangingVelocity)
            {
                bChangingVelocity = false;
            }

            else
            {
                Velocity = (transform.position - PreviousLocation) / DeltaTime;
            }

            return;
        }

        bIsGrounded = Physics.Raycast(transform.position, Vector3.down, Collider.height * 0.5f + 0.2f, WhatIsGround);
        Vector3 Delta;
        bool bSlideThisFrame = false;

        if(bIsGrounded)
        {
            if(Velocity.y < 0)
            {
                Velocity.y = 0;
            }

            Vector3 JumpVel = Vector3.zero;

            if (CurrentInput.SpaceBar && CurrentTimeStamp - LastTimeJumped > JumpCooldown)
            {
                LastTimeJumped = CurrentTimeStamp;

                JumpVel = Vector3.up * JumpForce;
            }

            if (CurrentInput.CTRL)
            {
                Delta = Velocity.magnitude >= SlideMoveSpeed ? (Velocity + JumpVel) * DeltaTime * (1 - SlideFriction * DeltaTime) : (Velocity.normalized * SlideMoveSpeed + JumpVel) * DeltaTime;

                bSlideThisFrame = true;

                if (!bIsSliding)
                {
                    bIsSliding = true;

                    if (IsOwner)
                    {
                        Player.CameraChangePosition(SlideCameraOffset, SlideCameraDuration);
                    }
                }

                if(!SlideSmoke.isPlaying)
                {
                    SlideSmoke.Play();
                }
            }

            else
            {
                Delta = (Velocity * (1 - GroundFriction * DeltaTime) + ((MoveDirection * WalkMoveSpeed) * (1 + GroundFriction) * DeltaTime) + JumpVel) * DeltaTime;
            }
        }

        else
        {
            Delta = (Velocity * (1 - AirFriction * DeltaTime) + ((MoveDirection * WalkMoveSpeed) * (1 + AirFriction) * DeltaTime) + (Gravity * Vector3.down)) * DeltaTime;
        }

        if(!bSlideThisFrame)
        {
            if(bIsSliding)
            {
                bIsSliding = false;

                Player.CameraResetPosition(SlideCameraDuration);
            }

            if(SlideSmoke.isPlaying)
            {
                SlideSmoke.Stop();
            }
        }

        SafeMovePlayer(Delta);

        if (bChangingVelocity)
        {
            bChangingVelocity = false;
        }

        else
        {
            Velocity = (transform.position - PreviousLocation) / DeltaTime;
        }
    }

    public void SafeMovePlayer(Vector3 delta)
    {
        transform.Translate(CollideAndSlide(transform.position, ref delta, 0));
    }

    Vector3 CollideAndSlide(Vector3 Pos, ref Vector3 Vel, int depth)
    {
        if(depth >= MaxBounces)
        {
            return Vector3.zero;
        }

        Vector3 p1 = Pos + ColliderOffset1;
        Vector3 p2 = p1 + ColliderOffset2;
        RaycastHit hit;

        if (Physics.CapsuleCast(
            p1,
            p2,
            bounds.extents.x,
            Vel.normalized,
            out hit,
            Vel.magnitude + SkinWidth,
            layerMask
            ))
        {
            Vector3 SnapToSurface = Vel.normalized * (hit.distance - SkinWidth);
            Vector3 Leftover = Vel - SnapToSurface;

            if(SnapToSurface.magnitude <= SkinWidth)
            {
                SnapToSurface = Vector3.zero;
            }

            Leftover = Vector3.ProjectOnPlane(Leftover, hit.normal);

            return SnapToSurface + CollideAndSlide(Pos + SnapToSurface, ref Leftover, depth + 1);
        }

        return Vel;
    }

/*
*
* Client Corrections
*
*/

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void SendClientDataServerRpc(int timestamp, Vector3 position)
    {
        if (timestamp > CurrentTimeStamp)
        {
            ClientDataDictionary.Add(timestamp, position);
        }

        Player.CheckClientTimeError(timestamp);
    }

    public void CheckClientPositionError(Vector3 serverpos, Vector3 clientpos)
    {
        if(Time.time - LastTimeSentCorrection < MinTimeBetweenCorrections)
        {
            return;
        }

        if ((clientpos - serverpos).magnitude <= CorrectionDistance)
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
        ClientCorrectionClientRpc(new ClientCorrection(
            CurrentTimeStamp,
            transform.position,
            Velocity,
            bNoMovement, 
            LastTimeJumped,
            bDashing,
            StartDashTime,
            DashingStartRotation), OwningClientID);
    }

    void SetToServerState()
    {
        transform.position = ServerState.Position;
        Velocity = ServerState.Velocity;
        bNoMovement = ServerState.bNoMovement;
        LastTimeJumped = ServerState.LastTimeJumped;
        bDashing = ServerState.bDashing;
        StartDashTime = ServerState.StartDashTime;
        DashingStartRotation = ServerState.DashingStartRotation;
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
        StartSmoothingCorrectionTime = CurrentTimeStamp;
        StartCorrectionPosition = transform.position;
    }

    void ReplayMovesAfterCorrection()
    {
        SetToServerState();

        int currentTime = CurrentTimeStamp;
        CurrentTimeStamp = SimulateTimeStamp + 1;

        while (CurrentTimeStamp < currentTime)
        {
            if (InputsDictionary.TryGetValue(CurrentTimeStamp, out Inputs inputs))
            {
                CurrentInput = inputs;
            }

            HandleInputs(ref CurrentInput);

            MovePlayer();

            CurrentTimeStamp++;
        }

        InputsDictionary.Clear();

        ReplayMoves = false;

        if ((transform.position - StartCorrectionPosition).magnitude < SmallCorrectionThreshold)
        {
            CorrectionSmoothTime = SmallCorrectionSmoothTime;

            return;
        }

        CorrectionSmoothTime = DefaultCorrectionSmoothTime;
    }

/*
*
* Inputs
*
*/

    private void CreateInputs(ref Inputs input)
    {
        input.TimeStamp = CurrentTimeStamp;
        input.Rotation = Rotation;
        input.W = Input.GetKey(KeyCode.W);
        input.A = Input.GetKey(KeyCode.A);
        input.S = Input.GetKey(KeyCode.S);
        input.D = Input.GetKey(KeyCode.D);
        input.SpaceBar = Input.GetKey(KeyCode.Space);
        input.Shift = Input.GetKey(KeyCode.LeftShift);
        input.CTRL = Input.GetKey(KeyCode.CapsLock);
    }

    private void HandleInputs(ref Inputs input)
    {
        MoveDirection = Vector3.zero;
        Rotation = input.Rotation;

        float a = Mathf.Sqrt((Rotation.w * Rotation.w) + (Rotation.y * Rotation.y));
        ForwardRotation = new Quaternion(0, Rotation.y / a, 0, Rotation.w / a);

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
        if(input.TimeStamp > CurrentTimeStamp)
        {
            InputsDictionary[input.TimeStamp] = input;
        }

        Player.CheckClientTimeError(input.TimeStamp);
    }

/*
*
* Replicating Movement
*
*/

        [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicatePositionClientRpc(Vector3 position, Vector3 velocity, Quaternion rotation, bool issliding, ClientRpcParams clientRpcParams = default)
    {
        if (Player.GetIsDead())
        {
            return;
        }

        bUpdatedThisFrame = true;

        transform.position = position;
        Velocity = velocity;

        ForwardRotation = new Quaternion(x: 0, y: rotation.y, z: 0, w: rotation.w / Mathf.Sqrt((rotation.w * rotation.w) + (rotation.y * rotation.y)));

        Orientation.transform.rotation = ForwardRotation;
        FPTransform.transform.rotation = rotation;

        bIsSliding = issliding;

        if (issliding && !SlideSmoke.isPlaying)
        {
            SlideSmoke.Play();
        }

        if (!issliding && SlideSmoke.isPlaying)
        {
            SlideSmoke.Stop();
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateDashingClientRpc(ClientRpcParams clientRpcParams = default)
    {
        ThirdPersonDashParticles.Play();
        DashTrails.emitting = true;
    }

    public Vector3 GetVelocity()
    {
        return Velocity;
    }

    public bool GetIsGrounded()
    {
        return bIsGrounded;
    }

    public Quaternion GetRotation()
    {
        return Rotation;
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public bool GetIsSliding()
    {
        return bIsSliding;
    }
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

            Leftover = ProjectAndScale(Leftover, hit.normal);
            */
