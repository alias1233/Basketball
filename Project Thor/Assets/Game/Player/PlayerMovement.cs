using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

struct ExternalMoveCorrection
{
    public Vector3 Position;
    public Vector3 Velocity;
    public int LastTimeJumped;
    public bool bWasSliding;
    public bool bWasGroundPounding;
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

    private PlayerManager Player;

    [SerializeField]
    private Transform FPOrientation;
    [SerializeField]
    private Transform TPOrientation;

    [SerializeField]
    private CameraVisualsScript CameraVisuals;

    [Header("Ticking")]

    private int CurrentTimeStamp;
    private float DeltaTime;

    [Header("Networking")]

    private NetworkRole LocalRole;
    private ClientRpcParams OwningClientID;
    private ClientRpcParams IgnoreOwnerRPCParams;

    [Header("Client Data")]

    private Dictionary<int, Inputs> InputsDictionary = new Dictionary<int, Inputs>();
    private Inputs CurrentInput;

    private int SaveRecentDataTime = 50;

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
    private bool bRewindingExternalMove;
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

    private float ColliderHeight;
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

    public float WalkMoveSpeed = 4;

    public float JumpForce = 10;
    public int JumpCooldown = 30;

    public float FlyMoveSpeed = 10;
    public float FlySprintSpeed = 15;
    public float FlyFriction = 5;
    public float FlyGravityFactor = 0.1f;
    public float FlyInputInfluence = 0.5f;

    public float SlideMoveSpeed = 8f;
    public float SlideJumpForce = 12f;
    public float SlideMinSpeed = 3f;
    public int SlideCooldown;

    public float GroundPoundSpeed = 15f;
    public float GroundPoundAcceleration;
    public float MinHeightToGroundPound = 1;
    public float MaxGroundPoundJumpForce = 1.75f;
    public float MaxGroundPoundChargeJumpTime = 50;

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
    private Vector3 JumpVel;
    private bool bWallRight;
    private bool bWallLeft;
    private RaycastHit RightWallHit;
    private RaycastHit LeftWallHit;

    private bool bChangingVelocity;

    public int StunDuration;

    private bool bStunned;
    private int LastTimeStunned = -99999;

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

    private bool bPrevR;

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

    public int FlyCooldown;
    public int FlyDuration;

    /*
    * 
    * Variables That Need To Be Sent For Client Corrections
    * 
    */

    private bool bDashing;
    private int StartDashTime;
    private Vector3 DashingStartRotation;

    private bool bGrapple;
    private int GrappleStartTime;
    private Vector3 GrappleLocation;

    private bool bFly;
    private int LastTimeFly;

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

    [SerializeField]
    private GameObject Wings;
    public GameObject FlightProgressBarObject;
    private ProgressBar FlightProgressBar;

    [Header("External Movement")]

    private ExternalMoveCorrection ExternalMoveServerState;

    private bool bRewindingGrappleMove;
    private GrappleMoveCorrection GrappleMoveServerState;

    private void Awake()
    {
        SelfTransform = transform;

        Player = GetComponent<PlayerManager>();
        CapsuleCollider Collider = GetComponent<CapsuleCollider>();

        ColliderOffset1 = Collider.center + Vector3.up * Collider.height * 0.5f + Vector3.down * Collider.radius;
        ColliderOffset2 = Collider.center + Vector3.down * Collider.height * 0.5f + Vector3.up * Collider.radius;
        CollidingRadius = Collider.radius - SkinWidth;
        ColliderHeight = Collider.height;

        DeltaTime = Time.fixedDeltaTime;

        FlightProgressBar = FlightProgressBarObject.GetComponent<ProgressBar>();
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
            OwningClientID = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { OwnerClientId }
                }
            };
        }
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
        HandleOwnerVisuals();
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

        HandleOtherPlayerVisuals();
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

        HandleOwnerVisuals();
    }

    private void ServerTickForAll()
    {
        if (Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
        {
            LastTimeReplicatedPosition = Time.time;

            if(bFly)
            {
                ReplicateFlyClientRpc(SelfTransform.position, Velocity, Rotation, IgnoreOwnerRPCParams);
            }

            else
            {
                ReplicatePositionClientRpc(SelfTransform.position, Velocity, Rotation, bSliding, bGroundPound, IgnoreOwnerRPCParams);
            }
        }
    }

    private void SimulatedProxyTick()
    {
        if (ThirdPersonDashParticles.isPlaying)
        {
            ThirdPersonDashParticles.transform.position = FPOrientation.position + Velocity.normalized * 2;
            ThirdPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - ThirdPersonDashParticles.transform.position), Vector3.up);
        }

        else
        {
            DashTrails.emitting = false;
        }

        if (bUpdatedThisFrame)
        {
            bUpdatedThisFrame = false;

            return;
        }

        SafeMovePlayer(Velocity * DeltaTime);
    }

    private void HandleOwnerVisuals()
    {
        if (bFly)
        {
            if (MoveDirection == Vector3.zero)
            {
                TPOrientation.rotation = Quaternion.RotateTowards(TPOrientation.rotation, ForwardRotation, 4f);
            }

            else
            {
                TPOrientation.rotation = Quaternion.LookRotation(Velocity.normalized, Vector3.up) * Quaternion.AngleAxis(60f, Vector3.right);
            }

            CameraVisuals.Offset(Mathf.Clamp(Velocity.magnitude, 0, 25));

            return;
        }

        if (bWallLeft)
        {
            CameraVisuals.Tilt(-6);

            return;
        }

        if (bWallRight)
        {
            CameraVisuals.Tilt(6);

            return;
        }

        if (CurrentInput.A && !CurrentInput.D)
        {
            if (bSliding)
            {
                CameraVisuals.Tilt(3);

                return;
            }

            if (bDashing)
            {
                CameraVisuals.Tilt(6);

                return;
            }

            CameraVisuals.Tilt(1.5f);

            return;
        }

        if (CurrentInput.D)
        {
            if (bSliding)
            {
                CameraVisuals.Tilt(-3);

                return;
            }

            if (bDashing)
            {
                CameraVisuals.Tilt(-6);

                return;
            }

            CameraVisuals.Tilt(-1.5f);

            return;
        }

        CameraVisuals.Tilt(0);
    }

    private void HandleOtherPlayerVisuals()
    {
        if (bFly)
        {
            if (MoveDirection == Vector3.zero)
            {
                TPOrientation.rotation = Quaternion.RotateTowards(TPOrientation.rotation, ForwardRotation, 4f);
            }

            else
            {
                TPOrientation.rotation = Quaternion.LookRotation(Velocity.normalized, Vector3.up) * Quaternion.AngleAxis(60f, Vector3.right);
            }
        }
    }

    private void AbilityTick()
    {
        if (CurrentInput.R && CurrentTimeStamp - LastTimeFly >= FlyCooldown)
        {
            EnterFly();

            bPrevR = true;
        }

        if (bFly)
        {
            if (IsOwner)
            {
                FlightProgressBar.UpdateProgressBar((float)(CurrentTimeStamp - LastTimeFly) / FlyDuration);
            }

            if (CurrentTimeStamp - LastTimeFly > FlyDuration || (CurrentInput.R && !bPrevR && CurrentTimeStamp - LastTimeFly > 50))
            {
                ExitFly();
            }
        }

        bPrevR = CurrentInput.R;

        if (CurrentInput.Shift && CurrentTimeStamp - StartDashTime >= DashCooldown && !bGrapple)
        {
            EnterDash();
        }

        if (bDashing)
        {
            if (CurrentTimeStamp - StartDashTime <= DashDuration)
            {
                SafeMovePlayer(DashingStartRotation * DashSpeed * DeltaTime);
            }

            else
            {
                ExitDash();
            }
        }

        if (CurrentInput.E && CurrentTimeStamp - GrappleShootTime >= GrappleShootCooldown)
        {
            ShootGrapple();
        }

        if (bGrapple)
        {
            if (CurrentTimeStamp - GrappleStartTime <= GrappleDuration && (GrappleLocation - SelfTransform.position).magnitude > 5)
            {
                Grapple();
            }

            else
            {
                ExitGrapple(false);
            }
        }
    }

    private void EnterFly()
    {
        bFly = true;
        LastTimeFly = CurrentTimeStamp;

        ExitSlide();
        ExitGroundPound(false);

        if(IsOwner)
        {
            StartCoroutine(CameraVisuals.EnterThirdPerson(0.1f));
            Player.EnterThirdPerson();
            FlightProgressBarObject.SetActive(true);
        }

        Wings.SetActive(true);
    }

    private void ExitFly()
    {
        bFly = false;

        bTrySlideGroundPound = false;

        if (IsOwner)
        {
            StartCoroutine(CameraVisuals.EnterFirstPerson(0.1f));
            Player.EnterFirstPerson();
            FlightProgressBarObject.SetActive(false);
        }

        Wings.SetActive(false);
        TPOrientation.rotation = ForwardRotation;
    }

    private void EnterDash()
    {
        StartDashTime = CurrentTimeStamp;
        bDashing = true;

        if (MoveDirection == Vector3.zero)
        {
            DashingStartRotation = ForwardRotation * Vector3.forward;
        }

        else
        {
            DashingStartRotation = MoveDirection;
        }

        bNoMovement = true;

        ExitGroundPound(false);
        ExitSlide();

        if (IsServer)
        {
            ReplicateDashingClientRpc(IgnoreOwnerRPCParams);
        }

        DashSound.Play();

        if (IsOwner)
        {
            FirstPersonDashParticles.transform.position = FPOrientation.position + DashingStartRotation * 2;
            FirstPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - FirstPersonDashParticles.transform.position), Vector3.up);
            FirstPersonDashParticles.Play();

            DashTrails.emitting = true;

            if(!bFly)
            {
                StartCoroutine(CameraVisuals.ChangeFOV(DashFOVOffset, DashFOVDuration));
            }
        }

        else
        {
            ThirdPersonDashParticles.transform.position = FPOrientation.position + DashingStartRotation * 2;
            ThirdPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - ThirdPersonDashParticles.transform.position), Vector3.up);
            ThirdPersonDashParticles.Play();

            DashTrails.emitting = true;
        }
    }

    private void ExitDash()
    {
        bDashing = false;
        bNoMovement = false;

        if(bFly)
        {
            Velocity *= AfterDashVelocityMagnitude * 2;
        }

        else
        {
            Velocity *= AfterDashVelocityMagnitude;
        }

        bChangingVelocity = true;

        if (IsOwner)
        {
            FirstPersonDashParticles.Stop();
        }

        DashTrails.emitting = false;
    }

    private void ShootGrapple()
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

    public void StartGrapple(Vector3 HitPos)
    {
        if (bStunned || Player.GetIsDead())
        {
            return;
        }

        if (Player.GetIsHoldingBall())
        {
            Ball.Singleton.Detach();
        }

        ExitGroundPound(false);
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

            StartCoroutine(CameraVisuals.ChangeFOV(GrappleFOVOffset, GrappleFOVDuration));
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

    private void Grapple()
    {
        SafeMovePlayer(GrappleSpeed * (GrappleLocation - SelfTransform.position).normalized * DeltaTime);

        if (bTryJump && CurrentTimeStamp - LastTimeJumped > JumpCooldown && CurrentTimeStamp - GrappleStartTime >= MinGrappleTimeBeforeJumping)
        {
            bTryJump = false;

            LastTimeJumped = CurrentTimeStamp;

            ExitGrapple(true);
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

    private void MovePlayer()
    {
        bWallRight = false;
        bWallLeft = false;

        PreviousPosition = SelfTransform.position;

        AbilityTick();

        if (bNoMovement)
        {
            if (bChangingVelocity)
            {
                bChangingVelocity = false;
            }

            else
            {
                Velocity = (SelfTransform.position - PreviousPosition) / DeltaTime;
            }

            return;
        }

        Vector3 Delta;

        if(bFly)
        {
            if(CurrentInput.CTRL)
            {
                Delta = (Velocity.magnitude * (Velocity.normalized + MoveDirection * FlyInputInfluence).normalized * (1 - FlyFriction * DeltaTime) + ((MoveDirection * FlySprintSpeed) * (1 + FlyFriction) * DeltaTime)) * DeltaTime;
            }

            else
            {
                Delta = (Velocity.magnitude * (Velocity.normalized + MoveDirection * FlyInputInfluence).normalized * (1 - FlyFriction * DeltaTime) + ((MoveDirection * FlyMoveSpeed) * (1 + FlyFriction) * DeltaTime)) * DeltaTime;
            }

            float GravityFactor = Vector3.Dot(Delta, Vector3.down);

            if (GravityFactor < 0)
            {
                Delta = Delta + GravityFactor * Velocity.normalized * FlyGravityFactor * 0.5f;
            }

            else
            {
                Delta = Delta + GravityFactor * Velocity.normalized * FlyGravityFactor;
            }
        }

        else
        {
            JumpVel = Vector3.zero;

            if (Physics.Raycast(SelfTransform.position, Vector3.down, ColliderHeight * 0.5f + 0.2f, WhatIsGround))
            {
                if (!bIsGrounded)
                {
                    EnterGrounded();
                }
            }

            else if (bIsGrounded)
            {
                bIsGrounded = false;
            }

            if (!bSliding)
            {
                if (bTrySlideGroundPound && Velocity.magnitude >= SlideMinSpeed && bIsGrounded && CurrentTimeStamp - LastTimeSlide >= SlideCooldown && LastTimeJumped != CurrentTimeStamp)
                {
                    EnterSlide();
                }
            }

            else if (!CurrentInput.CTRL || Velocity.magnitude < SlideMinSpeed)
            {
                ExitSlide();
            }

            if (!bGroundPound)
            {
                if (bTrySlideGroundPound && CurrentTimeStamp - LastTimeJumped > JumpCooldown * 2 && !Physics.Raycast(SelfTransform.position, Vector3.down, MinHeightToGroundPound, WhatIsGround))
                {
                    EnterGroundPound();
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

                        if (CurrentTimeStamp - TimeStartSlideGroundPound < 75)
                        {
                            JumpVel = (Vector3.up * 2.5f + SlideDirection).normalized * SlideJumpForce;

                            Velocity = Vector3.ClampMagnitude(Velocity, SlideMoveSpeed * 1.5f);
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
                    float Mag = Velocity.magnitude;

                    if (Mag < SlideMoveSpeed)
                    {
                        Delta = ((SlideDirection * SlideMoveSpeed)) * DeltaTime;
                    }

                    else if (Mag > SlideMoveSpeed * 2)
                    {
                        Delta = ((SlideDirection * SlideMoveSpeed * 2) * (1 - SlideFriction * DeltaTime)) * DeltaTime;
                    }

                    else
                    {
                        Delta = ((SlideDirection * Mag) * (1 - SlideFriction * DeltaTime)) * DeltaTime;
                    }
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

                else if (CurrentInput.A && !bGroundPound && CurrentTimeStamp - LastTimeJumped > JumpCooldown && Velocity.magnitude >= MinWallRunSpeed && CheckForLeftWall())
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
                    if (bGroundPound)
                    {
                        Delta = Velocity * DeltaTime;
                    }

                    else
                    {
                        Delta = (Velocity * (1 - AirFriction * DeltaTime) + ((MoveDirection * WalkMoveSpeed) * (1 + AirFriction) * DeltaTime) + (Gravity * Vector3.down)) * DeltaTime;
                    }
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

    private void EnterGrounded()
    {
        bIsGrounded = true;

        if (bGroundPound)
        {
            ExitGroundPound(true);

            if (bTryJump)
            {
                bTryJump = false;
                LastTimeJumped = CurrentTimeStamp;

                Velocity.y = 0;

                if (CurrentTimeStamp - TimeStartSlideGroundPound > MaxGroundPoundChargeJumpTime)
                {
                    JumpVel = Vector3.up * JumpForce * (MaxGroundPoundJumpForce + 0.25f);
                }

                else
                {
                    JumpVel = Vector3.up * JumpForce * (1.25f + (CurrentTimeStamp - TimeStartSlideGroundPound) / MaxGroundPoundChargeJumpTime * (MaxGroundPoundJumpForce - 1));
                }
            }
        }

        else if (Time.time - LastTimePlayedLandAudio >= LandAudioCooldown)
        {
            LastTimePlayedLandAudio = Time.time;
            LandAudio.Play();
        }
    }

    private void EnterSlide()
    {
        bTrySlideGroundPound = false;
        TimeStartSlideGroundPound = CurrentTimeStamp;
        bSliding = true;

        SlideDirection = MoveDirection;

        CameraVisuals.ChangePosition(SlideCameraOffset);

        SlideParticles.transform.position = SelfTransform.position + MoveDirection * 0.5f + Vector3.down * SlideParticleOffset;
        SlideParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - SlideParticles.transform.position), Vector3.up);
        SlideParticles.Play();
        SlideSmoke.Play();

        SlideSound.Play();
    }

    private void ExitSlide()
    {
        if (!bSliding)
        {
            return;
        }

        bSliding = false;
        LastTimeSlide = CurrentTimeStamp;

        CameraVisuals.ResetPosition();

        if (Time.time - LastTimePlayedSlideExitAudio >= LandAudioCooldown)
        {
            LastTimePlayedSlideExitAudio = Time.time;
            SlideExitSound.Play();
        }

        SlideParticles.Stop();
        SlideSmoke.Stop();
        SlideSound.Stop();
    }

    private void EnterGroundPound()
    {
        bTrySlideGroundPound = false;
        TimeStartSlideGroundPound = CurrentTimeStamp;
        bGroundPound = true;

        if (Velocity.y > 0)
        {
            Velocity.y = 0;
        }

        GroundPoundTrails.Play();

        Player.EnterDunk();
    }

    private void ExitGroundPound(bool bHitGround)
    {
        if(bGroundPound)
        {
            bGroundPound = false;

            if(bHitGround)
            {
                Velocity.x = 0;
                Velocity.z = 0;

                if (IsServer)
                {
                    int NumHits = Physics.OverlapSphereNonAlloc(SelfTransform.position, GroundPoundRadius, GroundPoundHits, PlayerLayer);

                    for (int i = 0; i < NumHits; i++)
                    {
                        GroundPoundHits[i].GetComponent<PlayerManager>().Damage(Player.GetTeam(), GroundPoundDamage);
                    }
                }

                if (IsOwner)
                {
                    StartCoroutine(CameraVisuals.Shake(1, 0.15f));
                }

                GroundPoundImpactParticle.Play();
                GroundPoundLandAudio.Play();
            }

            GroundPoundTrails.Stop();
            GroundPoundTrails.Clear();

            Player.ExitDunk();
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
            GrappleLocation,
            bPrevR,
            bFly,
            LastTimeFly
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
        bPrevR = ServerState.bPrevR;
        
        if(bFly && !ServerState.bFly)
        {
            ExitFly();
        }

        if(!bFly && ServerState.bFly)
        {
            EnterFly();
        }

        LastTimeFly = ServerState.LastTimeFly;
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

        else if(bRewindingExternalMove)
        {
            bRewindingExternalMove = false;

            SetToExternaMoveState();
        }

        else
        {
            SetToOnScoreState();

            bStunned = true;
            LastTimeStunned = SimulateTimeStamp;
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

        ReplicateExternalMovementClientrpc(CurrentTimeStamp, SelfTransform.position, Velocity,
            LastTimeJumped,
            bSliding,
            bGroundPound,
            OwningClientID);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateExternalMovementClientrpc(int replaytimestamp, Vector3 pos, Vector3 vel,
        int lasttimejumped, 
        bool bwassliding,
        bool bwasgroundpounding,
        ClientRpcParams clientRpcParams = default)
    {
        AfterCorrectionReceived(replaytimestamp);

        bRewindingExternalMove = true;

        ExternalMoveServerState.Position = pos;
        ExternalMoveServerState.Velocity = vel;
        ExternalMoveServerState.LastTimeJumped = lasttimejumped;
        ExternalMoveServerState.bWasSliding = bwassliding;
        ExternalMoveServerState.bWasGroundPounding = bwasgroundpounding;
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

        if(bGroundPound && !ExternalMoveServerState.bWasGroundPounding)
        {
            ExitGroundPound(false);
        }

        else
        {
            bGroundPound = ExternalMoveServerState.bWasGroundPounding;
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
        input.Shift = Input.GetKey(KeyCode.CapsLock);
        input.CTRL = Input.GetKey(KeyCode.LeftShift);
        input.E = Input.GetKey(KeyCode.E);
        input.R = Input.GetKey(KeyCode.R);
    }

    private void HandleInputs(ref Inputs input)
    {
        if (bStunned)
        {
            input.W = false;
            input.A = false;
            input.S = false;
            input.D = false;
            input.SpaceBar = false;
            input.Shift = false;
            input.CTRL = false;
            input.E = false;
            input.R = false;

            if (CurrentTimeStamp - LastTimeStunned > StunDuration)
            {
                bStunned = false;
            }
        }

        MoveDirection = Vector3.zero;
        Rotation = input.Rotation;

        float a = Mathf.Sqrt((Rotation.w * Rotation.w) + (Rotation.y * Rotation.y));
        ForwardRotation = new Quaternion(0, Rotation.y / a, 0, Rotation.w / a);

        if(bFly)
        {
            if (input.W)
            {
                MoveDirection += Rotation * Vector3.forward;
            }

            if (input.A)
            {
                MoveDirection -= Rotation * Vector3.right;
            }

            if (input.S)
            {
                MoveDirection -= Rotation * Vector3.forward;
            }

            if (input.D)
            {
                MoveDirection += Rotation * Vector3.right;
            }
        }

        else
        {
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

            if(!IsOwner)
            {
                TPOrientation.rotation = ForwardRotation;
            }
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
            EnterSlide();
        }

        if (!issliding && bSliding)
        {
            ExitSlide();
        }

        if(isgroundpounding && !bGroundPound)
        {
            bGroundPound = true;

            GroundPoundTrails.Play();
        }

        if(!isgroundpounding && bGroundPound)
        {
            ExitGroundPound(true);
        }

        if(bFly)
        {
            ExitFly();
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateFlyClientRpc(Vector3 position, Vector3 velocity, Quaternion rotation, ClientRpcParams clientRpcParams = default)
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

        if(!bFly)
        {
            EnterFly();
        }

        if (Velocity.magnitude < 2.5f)
        {
            TPOrientation.rotation = Quaternion.RotateTowards(TPOrientation.rotation, ForwardRotation, 4f);
        }

        else
        {
            TPOrientation.rotation = Quaternion.LookRotation(Velocity.normalized, Vector3.up) * Quaternion.AngleAxis(60f, Vector3.right);
        }

        if (bSliding)
        {
            ExitSlide();
        }

        if (bGroundPound)
        {
            ExitGroundPound(true);
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
            ExitGroundPound(false);

            return;
        }

        if (bExternalSource && !bNoMovement)
        {
            ReplicateExternalMovement();
        }

        ExitSlide();
    }

    public void OnScore(Vector3 Impulse)
    {
        ResetMovement();

        Velocity += Impulse;
        bStunned = true;
        LastTimeStunned = CurrentTimeStamp;

        ReplicateOnScore();

        if (IsOwner)
        {
            StartCoroutine(CameraVisuals.Shake(2, 1));
        }
    }

    private void ReplicateOnScore()
    {
        LastTimeSentCorrection = Time.time;

        ReplicateOnScoreClientrpc(CurrentTimeStamp, SelfTransform.position, Velocity, OwningClientID);

        if (IsOwner)
        {
            StartCoroutine(CameraVisuals.Shake(2, 1));
        }
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateOnScoreClientrpc(int replaytimestamp, Vector3 pos, Vector3 vel, ClientRpcParams clientRpcParams = default)
    {
        AfterCorrectionReceived(replaytimestamp);

        ExternalMoveServerState.Position = pos;
        ExternalMoveServerState.Velocity = vel;
    }

    private void SetToOnScoreState()
    {
        ResetMovement();

        SelfTransform.position = ExternalMoveServerState.Position;
        Velocity = ExternalMoveServerState.Velocity;
    }

    public void ResetMovement()
    {
        ExitSlide();
        ExitGrapple(false);
        ExitGroundPound(true);
        ExitDash();
        ExitFly();

        Velocity = Vector3.zero;
    }

    public void UpdateIgnoreOwnerRPCParams(ClientRpcParams newIgnoreOwnerRPCParams)
    {
        IgnoreOwnerRPCParams = newIgnoreOwnerRPCParams;
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

    public bool GetIsGroundPounding()
    {
        return bGroundPound;
    }

    public bool GetIsFlying()
    {
        return bFly;
    }

    public float GetLastTimeDash()
    {
        return StartDashTime;
    }

    public float GetLastTimeGrapple()
    {
        return GrappleShootTime;
    }

    public float GetLastTimeFly()
    {
        return LastTimeFly;
    }

    public ulong GetOwnerID()
    {
        return OwnerClientId;
    }

    public PlayerManager GetPlayer()
    {
        return Player;
    }
}
