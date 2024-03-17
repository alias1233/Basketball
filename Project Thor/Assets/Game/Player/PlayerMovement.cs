using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using static ConnectionNotificationManager;

struct ExternalMoveCorrection
{
    public Vector3 Position;
    public Vector3 Velocity;
    public int LastTimeJumped;
    public bool bWasSliding;
}

struct GrappleMoveCorrection
{
    public Vector3 Position;
    public int LastTimeJumped;
    public int GrappleStartTime;
    public Vector3 GrappleLocation;
}

public class PlayerMovement : NetworkBehaviour
{
    [Header("Cached Components")]

    private Transform SelfTransform;

    [Header("Components")]

    [SerializeField]
    private Transform TPOrientation;
    [SerializeField]
    private Transform FPOrientation;

    private PlayerManager Player;
    private WeaponManager Weapons;
    private CapsuleCollider Collider;

    [Header("Ticking")]

    private int CurrentTimeStamp;
    private float DeltaTime;

    [Header("Networking")]

    private NetworkRole LocalRole;
    private ClientRpcParams OwningClientID;
    private ClientRpcParams IgnoreOwnerRPCParams;
    private List<ulong> ClientIDList = new List<ulong>();

    [Header("Client Data")]

    private Dictionary<int, Inputs> InputsDictionary = new Dictionary<int, Inputs>();
    private Inputs CurrentInput;

    private int SaveRecentDataTime = 50;

    private bool bShift;

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
    private bool bRewindingClientCorrection;
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

    [Header("// Physics //")]

    public LayerMask layerMask;
    public int MaxBounces = 5;
    public float SkinWidth = 0.015f;

    private float CollidingRadius;
    private Vector3 ColliderOffset1;
    private Vector3 ColliderOffset2;

    public int MaxResolvePenetrationAttempts = 3;
    public float ResolvePenetrationDistance = 0.1f;
    private Collider[] Penetrations = new Collider[1];

    [Header("// Movement //")]

    public LayerMask WhatIsGround;
    public LayerMask PlayerLayer;
    public LayerMask PlayerObjectLayer;

    public float WalkMoveSpeed = 4f;
    
    public float JumpForce = 10f;
    public int JumpCooldown = 30;

    public float SlideMoveSpeed = 8f;
    public float SlideJumpForce = 12f;
    public float SlideMinSpeed = 3f;
    public int SlideCooldown;

    public float GroundPoundSpeed = 15f;
    public float GroundPoundAcceleration;
    public float MinHeightToGroundPound = 1;

    public float GroundPoundDamage;
    public float GroundPoundRadius;
    private Collider[] GroundPoundHits = new Collider[5];

    public float WallRunSpeed;
    public int WallJumpForce;
    public float MinWallRunSpeed;
    public float WallRunDampen;

    public float GroundFriction = 8;
    public float SlideFriction = 0.1f;
    public float WallRunFriction;
    public float AirFriction = 0.25f;
    public float Gravity = 1;

    private Vector3 PreviousPosition;
    private Quaternion Rotation;
    private Quaternion ForwardRotation;
    private Vector3 MoveDirection;
    private bool bIsGrounded;
    private bool bWallRight;
    private bool bWallLeft;
    private RaycastHit RightWallHit;
    private RaycastHit LeftWallHit;

    private bool bChangingVelocity;

    /*
    * 
    * Variables That Need To Be Sent For Client Corrections
    * 
    */

    private bool bNoMovement;
    private Vector3 Velocity;
    private int LastTimeJumped;
    private bool bWasSpace;
    private bool bTryJump;

    private bool bSliding;
    private Vector3 SlideDirection;
    private int LastTimeSlide;

    private bool bWasCTRL;
    private bool bTrySlideGroundPound;
    private int TimeStartSlideGroundPound;

    private bool bGroundPound;

    [Header("Visuals")]

    public AudioSource LandAudio;
    public float LandAudioCooldown;
    private float LastTimePlayedLandAudio;

    public Vector3 SlideCameraOffset;
    public float SlideCameraDuration;

    public float SlideParticleOffset;
    public ParticleSystem SlideParticles;
    public ParticleSystem SlideSmoke;

    public AudioSource SlideSound;
    public AudioSource SlideExitSound;
    private float LastTimePlayedSlideExitAudio;

    public ParticleSystem GroundPoundImpactParticle;
    public ParticleSystem GroundPoundTrails;
    public AudioSource GroundPoundLandAudio;

    [Header("// Abilities //")]

    public int DashDuration;
    public int DashCooldown;
    public float DashSpeed;
    public float AfterDashVelocityMagnitude;

    public NetworkObjectPool GrapplePool;

    public int GrappleDuration;
    public int GrappleShootCooldown;
    public float GrappleSpeed;
    public int MinGrappleTimeBeforeJumping;

    private int GrappleShootTime;

    /*
    * 
    * Variables That Need To Be Sent For Client Corrections
    * 
    */

    private bool bDashing;
    private int StartDashTime;
    private Quaternion DashingStartRotation;

    private bool bGrapple;
    private int GrappleStartTime;
    private Vector3 GrappleLocation;

    [Header("Visuals")]

    public float DashFOVOffset;
    public float DashFOVDuration;

    public ParticleSystem FirstPersonDashParticles;
    public ParticleSystem ThirdPersonDashParticles;
    public TrailRenderer DashTrails;

    public AudioSource DashSound;

    public float GrappleFOVOffset;
    public float GrappleFOVDuration;

    public AudioSource GrappleThrowStart;
    public AudioSource GrappleHit;
    public AudioSource GrappleLoop;

    [Header("External Movement")]

    private ExternalMoveCorrection ExternalMoveServerState;

    private bool bRewindingGrappleMove;
    private GrappleMoveCorrection GrappleMoveServerState;

    private void Awake()
    {
        Player = GetComponent<PlayerManager>();
        Weapons = GetComponent<WeaponManager>();
        Collider = GetComponent<CapsuleCollider>();

        SelfTransform = transform;

        ColliderOffset1 = Collider.center + Vector3.up * Collider.height * 0.5f + Vector3.down * Collider.radius;
        ColliderOffset2 = Collider.center + Vector3.down * Collider.height * 0.5f + Vector3.up * Collider.radius;
        CollidingRadius = Collider.radius - SkinWidth;

        DeltaTime = Time.fixedDeltaTime;
    }

    // Start is called before the first frame update
    private void Start()
    {
        LocalRole = Player.GetLocalRole();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            ConnectionNotificationManager.Singleton.OnClientConnectionNotification += UpdateClientSendRPCParams;

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

    // Called a fixed amount every second from PlayerManager with the current Timestamp
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

    private void HostTick()
    {
        CreateInputs(ref CurrentInput);
        HandleInputs(ref CurrentInput);

        MovePlayer();
    }

    private void ServerTickForOtherPlayers()
    {
        if (InputsDictionary.TryGetValue(CurrentTimeStamp, out Inputs inputs))
        {
            InputsDictionary.Remove(CurrentTimeStamp);
            CurrentInput = inputs;
        }

        HandleInputs(ref CurrentInput);
        FPOrientation.rotation = Rotation;

        MovePlayer();

        if (ClientDataDictionary.TryGetValue(CurrentTimeStamp, out Vector3 clientposition))
        {
            ClientDataDictionary.Remove(CurrentTimeStamp);
            CheckClientPositionError(SelfTransform.position, clientposition);
        }
    }

    private void AutonomousProxyTick()
    {
        if (bSmoothingPosition)
        {
            bSmoothingPosition = false;
            SelfTransform.position = CorrectedPosition;

            if (CurrentTimeStamp - StartSmoothingCorrectionTime >= CorrectionSmoothTime)
            {
                bSmoothingCorrection = false;
            }
        }

        if (ReplayMoves)
        {
            ReplayMovesAfterCorrection();
        }

        CreateInputs(ref CurrentInput);
        InputsDictionary[CurrentTimeStamp] = CurrentInput;
        InputsDictionary.Remove(CurrentTimeStamp - SaveRecentDataTime);
        SendInputsServerRpc(CurrentInput);
        HandleInputs(ref CurrentInput);

        MovePlayer();

        if (Time.time - LastTimeSentClientData > 0.33)
        {
            LastTimeSentClientData = Time.time;
            SendClientDataServerRpc(CurrentTimeStamp, SelfTransform.position);
        }

        if (bSmoothingCorrection)
        {
            bSmoothingPosition = true;
            CorrectedPosition = SelfTransform.position;
            SelfTransform.position = Vector3.Lerp(StartCorrectionPosition, CorrectedPosition, (float)(CurrentTimeStamp - StartSmoothingCorrectionTime) / CorrectionSmoothTime);
        }
    }

    private void ServerTickForAll()
    {
        if (Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
        {
            LastTimeReplicatedPosition = Time.time;
            ReplicatePositionClientRpc(SelfTransform.position, Velocity, Rotation, bSliding, bGroundPound, IgnoreOwnerRPCParams);
        }
    }

    private void SimulatedProxyTick()
    {
        if(ThirdPersonDashParticles.isPlaying)
        {
            ThirdPersonDashParticles.transform.position = FPOrientation.position + Velocity.normalized * 2;
            ThirdPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - ThirdPersonDashParticles.transform.position), Vector3.up);
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

    public void StartGrapple(Vector3 HitPos)
    {
        ExitSlide();

        bDashing = false;
        DashTrails.emitting = false;

        GrappleStartTime = CurrentTimeStamp;
        bGrapple = true;
        bNoMovement = true;

        GrappleLocation = HitPos;

        if (IsOwner)
        {
            FirstPersonDashParticles.transform.position = FPOrientation.position + (GrappleLocation - SelfTransform.position).normalized * 2;
            FirstPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - FirstPersonDashParticles.transform.position), Vector3.up);
            FirstPersonDashParticles.Play();

            Player.ChangeFOV(GrappleFOVOffset, GrappleFOVDuration);
        }

        else
        {
            ThirdPersonDashParticles.transform.position = FPOrientation.position + (GrappleLocation - SelfTransform.position).normalized * 2;
            ThirdPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - ThirdPersonDashParticles.transform.position), Vector3.up);
            ThirdPersonDashParticles.Play();
        }

        GrappleHit.Play();
        GrappleLoop.Play();

        if (IsServer)
        {
            ReplicateGrapple();
        }
    }

    private void ExitGrapple(bool bjump)
    {
        bGrapple = false;
        bNoMovement = false;

        Velocity = Velocity * 0.25f + Vector3.up * JumpForce * 1.25f;

        bChangingVelocity = true;

        if (IsOwner)
        {
            FirstPersonDashParticles.Stop();
        }

        GrappleLoop.Stop();
    }

    private void AbilityTick()
    {
        if (CurrentInput.Shift && CurrentTimeStamp - StartDashTime >= DashCooldown && !bGrapple)
        {
            StartDashTime = CurrentTimeStamp;
            bDashing = true;
            DashingStartRotation = Rotation;
            bNoMovement = true;

            ExitSlide();

            if (IsServer)
            {
                ReplicateDashingClientRpc(IgnoreOwnerRPCParams);
            }

            DashSound.Play();

            if (IsOwner)
            {
                FirstPersonDashParticles.transform.position = FPOrientation.position + DashingStartRotation * Vector3.forward * 2;
                FirstPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - FirstPersonDashParticles.transform.position), Vector3.up);
                FirstPersonDashParticles.Play();

                DashTrails.emitting = true;

                Player.ChangeFOV(DashFOVOffset, DashFOVDuration);
            }

            else
            {
                ThirdPersonDashParticles.transform.position = FPOrientation.position + DashingStartRotation * Vector3.forward * 2;
                ThirdPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - ThirdPersonDashParticles.transform.position), Vector3.up);
                ThirdPersonDashParticles.Play();

                DashTrails.emitting = true;
            }
        }

        if (bDashing)
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
                }

                DashTrails.emitting = false;
            }
        }

        if (CurrentInput.E && CurrentTimeStamp - GrappleShootTime >= GrappleShootCooldown)
        {
            GrappleShootTime = CurrentTimeStamp;

            GameObject obj = GrapplePool.GetPooledObject();

            if (obj != null)
            {
                GrapplingHook grapplinghook = obj.GetComponent<GrapplingHook>();

                grapplinghook.Spawn();

                if (IsServer && !IsOwner)
                {
                    grapplinghook.InitAndSimulateForward(Player.GetTeam(), SelfTransform.position + Vector3.up * 0.6f, Rotation * Vector3.forward, Player.GetHalfRTTInTick());
                }

                else
                {
                    grapplinghook.Init(Player.GetTeam(), SelfTransform.position + Vector3.up * 0.6f, Rotation * Vector3.forward);
                }
            }

            GrappleThrowStart.Play();
        }

        if (bGrapple)
        {
            if (CurrentTimeStamp - GrappleStartTime <= GrappleDuration && (GrappleLocation - SelfTransform.position).magnitude > 5)
            {
                SafeMovePlayer(GrappleSpeed * (GrappleLocation - SelfTransform.position).normalized * DeltaTime);

                if (bTryJump && CurrentTimeStamp - LastTimeJumped > JumpCooldown && CurrentTimeStamp - GrappleStartTime >= MinGrappleTimeBeforeJumping)
                {
                    bTryJump = false;

                    LastTimeJumped = CurrentTimeStamp;

                    ExitGrapple(true);
                }
            }

            else
            {
                ExitGrapple(false);
            }
        }
    }

    private void MovePlayer()
    {
        PreviousPosition = SelfTransform.position;

        AbilityTick();

        if(bNoMovement)
        {
            if(bChangingVelocity)
            {
                bChangingVelocity = false;
            }

            else
            {
                Velocity = (SelfTransform.position - PreviousPosition) / DeltaTime;
            }

            return;
        }

        Vector3 JumpVel = Vector3.zero;

        if (Physics.Raycast(SelfTransform.position, Vector3.down, Collider.height * 0.5f + 0.2f, WhatIsGround))
        {
            if(!bIsGrounded)
            {
                bIsGrounded = true;

                if(bGroundPound)
                {
                    ExitGroundPound();

                    if(bTryJump)
                    {
                        bTryJump = false;
                        LastTimeJumped = CurrentTimeStamp;

                        Velocity.y = 0;

                        //TimeStartSlideGroundPound
                        JumpVel = Vector3.up * JumpForce * 1.75f;
                    }
                }

                else if (Time.time - LastTimePlayedLandAudio >= LandAudioCooldown)
                {
                    LastTimePlayedLandAudio = Time.time;
                    LandAudio.Play();
                }
            }
        }

        else if (bIsGrounded)
        {
            bIsGrounded = false;
        }

        if (!bSliding)
        {
            if (bTrySlideGroundPound && Velocity.magnitude >= SlideMinSpeed && bIsGrounded && CurrentTimeStamp - LastTimeSlide >= SlideCooldown)
            {
                bTrySlideGroundPound = false;
                TimeStartSlideGroundPound = CurrentTimeStamp;
                bSliding = true;

                SlideDirection = MoveDirection;

                Player.CameraChangePosition(SlideCameraOffset, SlideCameraDuration);

                SlideParticles.transform.position = FPOrientation.position + MoveDirection * 2 + Vector3.down * SlideParticleOffset;
                SlideParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - SlideParticles.transform.position), Vector3.up);
                SlideParticles.Play();
                SlideSmoke.Play();

                SlideSound.Play();
            }
        }

        else if (!CurrentInput.CTRL || Velocity.magnitude < SlideMinSpeed)
        {
            ExitSlide();
        }

        if(!bGroundPound)
        {
            if(bTrySlideGroundPound && CurrentTimeStamp - LastTimeJumped > JumpCooldown && !Physics.Raycast(SelfTransform.position, Vector3.down, MinHeightToGroundPound, WhatIsGround))
            {
                bTrySlideGroundPound = false;
                TimeStartSlideGroundPound = CurrentTimeStamp;
                bGroundPound = true;

                if (Velocity.y > 0)
                {
                    Velocity.y = 0;
                }

                GroundPoundTrails.Play();

                Weapons.EnterDunk();
            }
        }

        else
        {
            Velocity.y -= GroundPoundAcceleration * DeltaTime;

            if (Velocity.y < -GroundPoundSpeed)
            {
                Velocity.y = -GroundPoundSpeed;
            }
        }

        Vector3 Delta;

        if (bIsGrounded)
        {
            if (bTryJump && CurrentTimeStamp - LastTimeJumped > JumpCooldown)
            {
                LastTimeJumped = CurrentTimeStamp;
                bTryJump = false;

                Velocity.y = 0;

                if (bSliding)
                {
                    ExitSlide();

                    if(CurrentTimeStamp - TimeStartSlideGroundPound < 50)
                    {
                        JumpVel = (Vector3.up * 2.5f + SlideDirection).normalized * SlideJumpForce;
                    }

                    else
                    {
                        JumpVel = Vector3.up * JumpForce * 1.75f;
                    }
                }

                else
                {
                    JumpVel = Vector3.up * JumpForce;
                }
            }

            if (bSliding)
            {
                Delta = Velocity.magnitude >= SlideMoveSpeed
                    ? ((SlideDirection * Velocity.magnitude) * (1 - SlideFriction * DeltaTime) + JumpVel) * DeltaTime
                    : ((SlideDirection * SlideMoveSpeed) + JumpVel) * DeltaTime;
            }

            else
            {
                Delta = (Velocity * (1 - GroundFriction * DeltaTime) + ((MoveDirection * WalkMoveSpeed) * (1 + GroundFriction) * DeltaTime) + JumpVel) * DeltaTime;
            }
        }

        else
        {
            if (CurrentInput.D && !bGroundPound && CurrentTimeStamp - LastTimeJumped > JumpCooldown && Velocity.magnitude >= MinWallRunSpeed && CheckForRightWall())
            {
                ExitSlide();

                Vector3 WallNormal = RightWallHit.normal;
                Vector3 WallForward = Vector3.Cross(WallNormal, Vector3.up);
                Vector3 ForwardVector = ForwardRotation * Vector3.forward;

                Velocity.y *= 1 - WallRunDampen * DeltaTime;

                if ((ForwardVector - WallForward).magnitude > (ForwardVector + WallForward).magnitude)
                {
                    WallForward = -WallForward;
                }

                if (bTryJump)
                {
                    LastTimeJumped = CurrentTimeStamp;
                    bTryJump = false;
                    JumpVel = (WallNormal + 2 * Vector3.up) * WallJumpForce;
                }

                Delta = (Velocity * (1 - WallRunFriction * DeltaTime) + ((WallForward * WallRunSpeed) * (1 + WallRunFriction) * DeltaTime) + JumpVel) * DeltaTime;
            }

            else if(CurrentInput.A && !bGroundPound && CurrentTimeStamp - LastTimeJumped > JumpCooldown && Velocity.magnitude >= MinWallRunSpeed && CheckForLeftWall())
            {
                ExitSlide();

                Vector3 WallNormal = LeftWallHit.normal;
                Vector3 WallForward = Vector3.Cross(WallNormal, Vector3.up);
                Vector3 ForwardVector = ForwardRotation * Vector3.forward;

                Velocity.y *= 1 - WallRunDampen * DeltaTime;

                if ((ForwardVector - WallForward).magnitude > (ForwardVector + WallForward).magnitude)
                {
                    WallForward = -WallForward;
                }

                if (bTryJump)
                {
                    LastTimeJumped = CurrentTimeStamp;
                    bTryJump = false;
                    JumpVel = (WallNormal + 2 * Vector3.up) * WallJumpForce;
                }

                Delta = (Velocity * (1 - WallRunFriction * DeltaTime) + ((WallForward * WallRunSpeed) * (1 + WallRunFriction) * DeltaTime) + JumpVel) * DeltaTime;
            }

            else
            {
                if(bGroundPound)
                {
                    Delta = Velocity * DeltaTime;
                }

                else
                {
                    Delta = (Velocity * (1 - AirFriction * DeltaTime) + ((MoveDirection * WalkMoveSpeed) * (1 + AirFriction) * DeltaTime) + (Gravity * Vector3.down)) * DeltaTime;
                }
            }
        }

        SafeMovePlayer(Delta);

        if (bChangingVelocity)
        {
            bChangingVelocity = false;
        }

        else
        {
            Velocity = (SelfTransform.position - PreviousPosition) / DeltaTime;
        }
    }

    private bool CheckForRightWall()
    {
        return bWallRight = Physics.Raycast(SelfTransform.position, ForwardRotation * Vector3.right, out RightWallHit, 1, WhatIsGround);
    }

    private bool CheckForLeftWall()
    {
        return bWallLeft = Physics.Raycast(SelfTransform.position, -(ForwardRotation * Vector3.right), out LeftWallHit, 1, WhatIsGround);
    }

    private void ExitSlide()
    {
        if (!bSliding)
        {
            return;
        }

        bSliding = false;
        LastTimeSlide = CurrentTimeStamp;

        Player.CameraResetPosition(SlideCameraDuration);

        if (Time.time - LastTimePlayedSlideExitAudio >= LandAudioCooldown)
        {
            LastTimePlayedSlideExitAudio = Time.time;
            SlideExitSound.Play();
        }

        SlideParticles.Stop();
        SlideSmoke.Stop();
        SlideSound.Stop();
    }

    private void ExitGroundPound()
    {
        bGroundPound = false;

        GroundPoundImpactParticle.Play();

        GroundPoundTrails.Stop();
        GroundPoundTrails.Clear();
        GroundPoundLandAudio.Play();

        Weapons.ExitDunk();

        if (IsServer)
        {
            int NumHits = Physics.OverlapSphereNonAlloc(SelfTransform.position, GroundPoundRadius, GroundPoundHits, PlayerLayer);

            for (int i = 0; i < NumHits; i++)
            {
                GroundPoundHits[i].GetComponent<PlayerManager>().Damage(Player.GetTeam(), GroundPoundDamage);
            }
        }
    }

    private void SafeMovePlayer(Vector3 delta)
    {
        SelfTransform.position += CollideAndSlide(SelfTransform.position, delta, 0);
    }

    private Vector3 CollideAndSlide(Vector3 Pos, Vector3 Vel, int depth)
    {
        if (depth >= MaxBounces)
        {
            return Vector3.zero;
        }

        if (Physics.CapsuleCast(
            Pos + ColliderOffset1,
            Pos + ColliderOffset2,
            CollidingRadius,
            Vel,
            out RaycastHit hit,
            Vel.magnitude + SkinWidth,
            layerMask
            ))
        {

            Vector3 SnapToSurface = Vel.normalized * (hit.distance + SkinWidth / Mathf.Cos(Vector3.Angle(Vel, hit.normal) * Mathf.PI / 180));

            return SnapToSurface + CollideAndSlide(Pos + SnapToSurface, Vector3.ProjectOnPlane(Vel - SnapToSurface, hit.normal), depth + 1);
        }

        int PenetrationAttempts = 1;

        while (Physics.OverlapCapsuleNonAlloc(
            Pos + ColliderOffset1,
            Pos + ColliderOffset2,
            CollidingRadius,
            Penetrations,
            layerMask
            )
            == 1 && PenetrationAttempts <= MaxResolvePenetrationAttempts + 1)
        {
            Vector3 ResolvePenetration = (Pos - Penetrations[0].ClosestPoint(Pos)).normalized * ResolvePenetrationDistance * PenetrationAttempts * PenetrationAttempts;

            if (ResolvePenetration == Vector3.zero)
            {
                ResolvePenetration = (Pos - Penetrations[0].bounds.center).normalized * ResolvePenetrationDistance * PenetrationAttempts * PenetrationAttempts;
            }

            Vel += ResolvePenetration;
            Pos += ResolvePenetration;
            PreviousPosition += ResolvePenetration;

            PenetrationAttempts++;
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

    private void CheckClientPositionError(Vector3 serverpos, Vector3 clientpos)
    {
        if(Time.time - LastTimeSentCorrection < MinTimeBetweenCorrections)
        {
            return;
        }

        if ((clientpos - serverpos).magnitude <= CorrectionDistance)
        {
            SelfTransform.position = clientpos;

            return;
        }

        SendClientCorrection();
    }

    public void SendClientCorrection()
    {
        LastTimeSentCorrection = Time.time;

        ClientCorrectionClientRpc(new ClientCorrection(
            CurrentTimeStamp,
            SelfTransform.position,
            Velocity,
            bNoMovement,
            LastTimeJumped,
            bWasSpace,
            bTryJump,
            bDashing,
            StartDashTime,
            DashingStartRotation,
            bSliding,
            SlideDirection,
            LastTimeSlide,
            bWasCTRL,
            bTrySlideGroundPound,
            TimeStartSlideGroundPound,
            bGroundPound,
            bGrapple,
            GrappleStartTime,
            GrappleLocation
            ),
            OwningClientID);

        print("Sent Client Correction!" + CurrentTimeStamp);
    }

    private void SetToServerState()
    {
        SelfTransform.position = ServerState.Position;
        Velocity = ServerState.Velocity;
        bNoMovement = ServerState.bNoMovement;
        LastTimeJumped = ServerState.LastTimeJumped;
        bWasSpace = ServerState.bWasSpace;
        bTryJump = ServerState.bTryJump;
        bDashing = ServerState.bDashing;
        StartDashTime = ServerState.StartDashTime;
        DashingStartRotation = ServerState.DashingStartRotation;

        if (bSliding && !ServerState.bSliding)
        {
            ExitSlide();
        }

        else
        {
            bSliding = ServerState.bSliding;
        }

        SlideDirection = ServerState.SlideDirection;
        LastTimeSlide = ServerState.LastTimeSlide;
        bWasCTRL = ServerState.bWasCTRL;
        bTrySlideGroundPound = ServerState.bTrySlideGroundPound;
        TimeStartSlideGroundPound = ServerState.TimeStartSlideGroundPound;
        bGroundPound = ServerState.bGroundPound;
        bGrapple = ServerState.bGrapple;
        GrappleStartTime = ServerState.GrappleStartTime;
        GrappleLocation = ServerState.GrappleLocation;
    }

    private void ReplayMovesAfterCorrection()
    {
        if (bRewindingClientCorrection)
        {
            bRewindingClientCorrection = false;

            SetToServerState();
        }

        else if(bRewindingGrappleMove)
        {
            bRewindingGrappleMove = false;

            SetToGrappleState();
        }

        else
        {
            SetToExternaMoveState();
        }

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

        if ((SelfTransform.position - StartCorrectionPosition).magnitude < SmallCorrectionThreshold)
        {
            CorrectionSmoothTime = SmallCorrectionSmoothTime;

            return;
        }

        CorrectionSmoothTime = DefaultCorrectionSmoothTime;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ClientCorrectionClientRpc(ClientCorrection Data, ClientRpcParams clientRpcParams = default)
    {
        bRewindingClientCorrection = true;
        AfterCorrectionReceived(Data.TimeStamp);
        ServerState = Data;
    }

    private void AfterCorrectionReceived(int replaytimestamp)
    {
        ReplayMoves = true;
        SimulateTimeStamp = replaytimestamp;

        bSmoothingCorrection = true;
        StartSmoothingCorrectionTime = CurrentTimeStamp;
        StartCorrectionPosition = SelfTransform.position;
    }

    private void ReplicateExternalMovement()
    {
        LastTimeSentCorrection = Time.time;

        ReplicateReplicateExternalMovementClientrpc(CurrentTimeStamp, SelfTransform.position, Velocity, LastTimeJumped, bSliding, OwningClientID);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateReplicateExternalMovementClientrpc(int replaytimestamp, Vector3 pos, Vector3 vel, int lasttimejumped, bool bwassliding, ClientRpcParams clientRpcParams = default)
    {
        AfterCorrectionReceived(replaytimestamp);

        ExternalMoveServerState.Position = pos;
        ExternalMoveServerState.Velocity = vel;
        ExternalMoveServerState.LastTimeJumped = lasttimejumped;
        ExternalMoveServerState.bWasSliding = bwassliding;
    }

    private void SetToExternaMoveState()
    {
        SelfTransform.position = ExternalMoveServerState.Position;
        Velocity = ExternalMoveServerState.Velocity;
        LastTimeJumped = ExternalMoveServerState.LastTimeJumped;

        if (ExternalMoveServerState.bWasSliding)
        {
            ExitSlide();

            LastTimeSlide = SimulateTimeStamp;
        }
    }

    private void ReplicateGrapple()
    {
        LastTimeSentCorrection = Time.time;

        ReplicateGrappleClientrpc(CurrentTimeStamp, SelfTransform.position, LastTimeJumped,
            GrappleStartTime,
            GrappleLocation,
            OwningClientID
            );
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateGrappleClientrpc(int replaytimestamp, Vector3 pos, int lasttimejumped,
        int grappleStartTime, Vector3 grappleLocation, ClientRpcParams clientRpcParams = default)
    {
        AfterCorrectionReceived(replaytimestamp);

        bRewindingGrappleMove = true;

        GrappleMoveServerState.Position = pos;
        GrappleMoveServerState.LastTimeJumped = lasttimejumped;
        GrappleMoveServerState.GrappleStartTime = grappleStartTime;
        GrappleMoveServerState.GrappleLocation = grappleLocation;
    }

    public void SetToGrappleState()
    {
        StartGrapple(GrappleMoveServerState.GrappleLocation);

        SelfTransform.position = GrappleMoveServerState.Position;
        LastTimeJumped = GrappleMoveServerState.LastTimeJumped;
        GrappleStartTime = GrappleMoveServerState.GrappleStartTime;
    }

    /*
    *
    * Inputs
    *
    */

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.CapsLock))
        {
            bShift = true;
        }
    }

    private void CreateInputs(ref Inputs input)
    {
        Rotation = FPOrientation.rotation;

        input.TimeStamp = CurrentTimeStamp;
        input.Rotation = Rotation;
        input.W = Input.GetKey(KeyCode.W);
        input.A = Input.GetKey(KeyCode.A);
        input.S = Input.GetKey(KeyCode.S);
        input.D = Input.GetKey(KeyCode.D);
        input.SpaceBar = Input.GetKey(KeyCode.Space);
        input.Shift = bShift;
        input.CTRL = Input.GetKey(KeyCode.LeftShift);
        input.E = Input.GetKey(KeyCode.E);

        bShift = false;
    }

    private void HandleInputs(ref Inputs input)
    {
        MoveDirection = Vector3.zero;
        Rotation = input.Rotation;

        float a = Mathf.Sqrt((Rotation.w * Rotation.w) + (Rotation.y * Rotation.y));
        ForwardRotation = new Quaternion(0, Rotation.y / a, 0, Rotation.w / a);

        TPOrientation.rotation = ForwardRotation;

        if (input.W)
        {
            MoveDirection += ForwardRotation * Vector3.forward;
        }

        if (input.A)
        {
            MoveDirection -= ForwardRotation * Vector3.right;
        }

        if (input.S)
        {
            MoveDirection -= ForwardRotation * Vector3.forward;
        }

        if (input.D)
        {
            MoveDirection += ForwardRotation * Vector3.right;
        }

        MoveDirection.Normalize();

        if (input.CTRL && !bWasCTRL)
        {
            bTrySlideGroundPound = true;
        }

        else if (!input.CTRL)
        {
            bTrySlideGroundPound = false;
        }

        bWasCTRL = input.CTRL;

        if (input.SpaceBar && !bWasSpace)
        {
            bTryJump = true;
        }

        else if(!input.SpaceBar)
        {
            bTryJump = false;
        }

        bWasSpace = input.SpaceBar;
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
    public void ReplicatePositionClientRpc(Vector3 position, Vector3 velocity, Quaternion rotation, bool issliding, bool isgroundpounding, ClientRpcParams clientRpcParams = default)
    {
        if (Player.GetIsDead())
        {
            return;
        }

        bUpdatedThisFrame = true;

        SelfTransform.position = position;
        Velocity = velocity;

        Rotation = rotation;
        float a = Mathf.Sqrt((rotation.w * rotation.w) + (rotation.y * rotation.y));
        ForwardRotation = new Quaternion(0, rotation.y / a, 0, rotation.w / a);

        TPOrientation.rotation = ForwardRotation;
        FPOrientation.rotation = rotation;

        if (issliding && !bSliding)
        {
            bSliding = true;

            Player.CameraChangePosition(SlideCameraOffset, SlideCameraDuration);

            SlideParticles.transform.position = FPOrientation.position + Velocity.normalized * 2 + Vector3.down * SlideParticleOffset;
            SlideParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - SlideParticles.transform.position), Vector3.up);
            SlideParticles.Play();
            SlideSmoke.Play();

            SlideSound.Play();
        }

        if (!issliding && bSliding)
        {
            bSliding = false;

            Player.CameraResetPosition(SlideCameraDuration);
            SlideParticles.Stop();
            SlideSmoke.Stop();
            SlideSound.Stop();
        }

        if(isgroundpounding && !bGroundPound)
        {
            bGroundPound = true;

            GroundPoundTrails.Play();
        }

        if(!isgroundpounding && bGroundPound)
        {
            ExitGroundPound();
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateDashingClientRpc(ClientRpcParams clientRpcParams = default)
    {
        ThirdPersonDashParticles.Play();
        DashTrails.emitting = true;

        DashSound.Play();
    }

    public void AddVelocity(Vector3 Impulse, bool bExternalSource)
    {
        Velocity += Impulse;

        if (IsOwner)
        {
            ExitSlide();

            return;
        }

        if (bExternalSource && !bNoMovement)
        {
            ReplicateExternalMovement();
        }

        ExitSlide();
    }

    public void ChangeVelocity(Vector3 NewVel)
    {
        Velocity = NewVel;
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

    public void SetRotation(Quaternion newrot)
    {
        Rotation = newrot;
    }

    public Vector3 GetPosition()
    {
        return SelfTransform.position;
    }

    public bool GetIsSliding()
    {
        return bSliding;
    }

    public float GetLastTimeDash()
    {
        return StartDashTime;
    }

    public ulong GetOwnerID()
    {
        return OwnerClientId;
    }
}
