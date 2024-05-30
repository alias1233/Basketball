using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

struct ExternalMoveState
{
    public Vector3 Position;
    public Vector3 Velocity;
}

public class BaseCharacterMovement : NetworkBehaviour
{
    [Header("Cached Components")]

    protected Transform SelfTransform;

    [Header("Components")]

    protected BasePlayerManager Player;

    [SerializeField]
    protected BaseCharacterComponents Components;

    protected Transform FPOrientation;
    protected Transform TPOrientation;
    protected CameraVisualsScript CameraVisuals;

    [Header("Ticking")]

    protected int CurrentTimeStamp;
    protected float DeltaTime;

    [Header("Networking")]

    protected NetworkRole LocalRole;
    protected ClientRpcParams OwningClientID;
    protected ClientRpcParams IgnoreOwnerRPCParams;

    [Header("Client Data")]

    private Dictionary<int, Inputs> InputsDictionary = new Dictionary<int, Inputs>();
    protected Inputs CurrentInput;

    private int SaveRecentDataTime = 50;

    [Header("Replicate Movement")]

    public float ReplicatePositionInterval = 0.05f;

    private float LastTimeReplicatedPosition;
    protected bool bUpdatedThisFrame;

    [Header("Client Corrections")]

    public float CorrectionDistance = 0.5f;
    public float MinTimeBetweenCorrections = 1;
    public int DefaultCorrectionSmoothTime = 8;
    public float SmallCorrectionThreshold = 3;
    public int SmallCorrectionSmoothTime = 6;

    private Dictionary<int, Vector3> ClientDataDictionary = new Dictionary<int, Vector3>();
    private ExternalMoveState ServerExternalMoveState;
    protected bool bRewindingClientCorrection;
    protected float LastTimeSentCorrection;
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

    public LayerMask PhysicsLayerMask;
    public int MaxBounces = 5;
    public float SkinWidth = 0.015f;

    private float ColliderHeight;
    private float CollidingRadius;
    private Vector3 ColliderOffset1;
    private Vector3 ColliderOffset2;

    public int MaxResolvePenetrationAttempts = 5;
    public float ResolvePenetrationDistance = 0.05f;
    private Collider[] Penetrations = new Collider[1];

    [Header("// Movement //")]

    public LayerMask WhatIsGround;
    public LayerMask PlayerLayer;
    public LayerMask PlayerObjectLayer;

    public float WalkMoveSpeed = 5;

    public float JumpForce = 25;
    public int JumpCooldown = 8;

    public float SlideMoveSpeed = 10f;
    public float SlideJumpForce = 18f;
    public float SlideChargeJumpMagnitude = 1.75f;
    public float SlideMinSpeed = 3f;
    public int SlideCooldown = 7;
    public int SlideChargeJumpTime = 65;

    public float GroundPoundSpeed = 25f;
    public float GroundPoundAcceleration = 250;
    public float MinHeightToGroundPound = 2.5f;
    public float MaxGroundPoundJumpForce = 1.75f;
    public float MaxGroundPoundChargeJumpTime = 50;

    public float GroundPoundDamage;
    public float GroundPoundRadius;
    private Collider[] GroundPoundHits = new Collider[5];

    public float WallRunSpeed = 13;
    public int WallJumpForce = 10;
    public float MinWallRunSpeed = 2;
    public float WallRunDampen = 15;
    public float WallRunCheckForWallRange = 1;

    public float GroundFriction = 15;
    public float SlideFriction = 0.75f;
    public float WallRunFriction = 10;
    public float AirFriction = 1f;
    public float Gravity = 0.75f;

    protected Vector3 PreviousPosition;
    protected Quaternion Rotation;
    protected Quaternion ForwardRotation;
    protected Vector3 MoveDirection;
    protected bool bIsGrounded;
    protected Vector3 JumpVel;
    protected bool bWallRight;
    protected bool bWallLeft;
    protected RaycastHit RightWallHit;
    protected RaycastHit LeftWallHit;

    protected bool bChangingVelocity;

    public int StunDuration = 50;

    protected bool bStunned;
    protected int LastTimeStunned = -99999;

    /*
    * 
    * Variables That Need To Be Sent For Client Corrections
    * 
    */

    protected bool bNoMovement;
    protected Vector3 Velocity;
    protected int LastTimeJumped;
    protected bool bWasSpace;
    protected bool bTryJump;

    protected bool bSliding;
    protected Vector3 SlideDirection;
    protected int LastTimeSlide;

    protected bool bWasCTRL;
    protected bool bTrySlideGroundPound;
    protected int TimeStartSlideGroundPound;

    protected bool bGroundPound;

    [Header("Visuals")]

    public float VelocityFOVFactor;
    public float VelocityFOVSpeed;
    public float CameraTiltYSpeed;

    public float HeadBobVelocityFactor;
    public float HeadBobAmplitude;
    public float ResetHeadBobSpeed;

    protected AudioSource LandAudio;
    public float LandAudioCooldown = 0.5f;
    private float LastTimePlayedLandAudio;
    protected AudioSource JumpAudio;
    protected AudioSource BoostedJumpAudio;

    public Vector3 SlideCameraOffset;
    public float SlideRotationOffset = 15f;
    public float SlideCameraDuration = 20;
    public float SlideCameraResetDuration = 10;
    private float SlideCameraSpeed;
    private float SlideCameraResetSpeed;
    protected GameObject SlideChargeJumpUIObject;
    protected ProgressBar SlideChargeJumpProgressBar;

    public float SlideParticleOffset;
    protected ParticleSystem SlideParticles;
    protected ParticleSystem SlideSmoke;

    protected AudioSource SlideSound;
    protected AudioSource SlideExitSound;
    private float LastTimePlayedSlideExitAudio;

    protected ParticleSystem GroundPoundImpactParticle;
    protected ParticleSystem GroundPoundTrails;
    protected AudioSource GroundPoundLandAudio;

    protected virtual void Awake()
    {
        SelfTransform = transform;

        Player = GetComponent<BasePlayerManager>();
        FPOrientation = Components.FPOrientation;
        TPOrientation = Components.TPOrientation;
        CameraVisuals = Components.CameraVisuals;
        LandAudio = Components.LandAudio;
        JumpAudio = Components.JumpAudio;
        BoostedJumpAudio = Components.BoostedJumpAudio;
        SlideChargeJumpUIObject = Components.SlideChargeJumpUIObject;
        SlideChargeJumpProgressBar = Components.SlideChargeJumpProgressBar;
        SlideParticles = Components.SlideParticles;
        SlideSmoke = Components.SlideSmoke;
        SlideSound = Components.SlideSound;
        SlideExitSound = Components.SlideExitSound;
        GroundPoundImpactParticle = Components.GroundPoundImpactParticle;
        GroundPoundTrails = Components.GroundPoundTrails;
        GroundPoundLandAudio = Components.GroundPoundLandAudio;

        CapsuleCollider Collider = GetComponent<CapsuleCollider>();
        ColliderOffset1 = Collider.center + Collider.height * 0.5f * Vector3.up + Vector3.down * Collider.radius;
        ColliderOffset2 = Collider.center + Collider.height * 0.5f * Vector3.down + Vector3.up * Collider.radius;
        CollidingRadius = Collider.radius - SkinWidth;
        ColliderHeight = Collider.height;

        DeltaTime = Time.fixedDeltaTime;

        SlideCameraSpeed = (FPOrientation.localPosition - SlideCameraOffset).magnitude / SlideCameraDuration;
        SlideCameraResetSpeed = (FPOrientation.localPosition - SlideCameraOffset).magnitude / SlideCameraResetDuration;
    }

    // Start is called before the first frame update
    protected virtual void Start()
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

    public void HostOwnerTick(int timestamp)
    {
        CurrentTimeStamp = timestamp;

        HostTick();
        ServerTickForAll();
    }

    public void HostProxyTick(int timestamp)
    {
        CurrentTimeStamp = timestamp;

        ServerTickForOtherPlayers();
        ServerTickForAll();
    }

    public void AutonomousProxyTick(int timestamp)
    {
        CurrentTimeStamp = timestamp;

        ClientOwnerTick();
    }

    public void SimulatedProxyTick(int timestamp)
    {
        CurrentTimeStamp = timestamp;

        ClientProxyTick();
    }

    protected virtual void HostTick()
    {
        CreateInputs(ref CurrentInput);
        HandleInputs(ref CurrentInput);

        MovePlayer();
        HandleOwnerVisuals();
    }

    protected virtual void ServerTickForOtherPlayers()
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

            if (Time.time - LastTimeSentCorrection > MinTimeBetweenCorrections)
            {
                CheckClientPositionError(SelfTransform.position, clientposition);
            }
        }

        HandleOtherPlayerVisuals();
    }

    protected virtual void ClientOwnerTick()
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

    protected virtual void ServerTickForAll()
    {
        if (Time.time - LastTimeReplicatedPosition >= ReplicatePositionInterval)
        {
            LastTimeReplicatedPosition = Time.time;

            ReplicateMovement();
        }
    }

    protected virtual void ReplicateMovement()
    {
        ReplicatePositionClientRpc(SelfTransform.position, Velocity, Rotation, bSliding, bGroundPound, IgnoreOwnerRPCParams);
    }

    protected virtual void ClientProxyTick()
    {
        if (bUpdatedThisFrame)
        {
            bUpdatedThisFrame = false;

            return;
        }

        SafeMovePlayer(Velocity * DeltaTime);
    }

    private void HandleOwnerVisuals()
    {
        if(CanHeadBob())
        {
            CameraVisuals.HeadBob((Velocity.magnitude + 10) * HeadBobVelocityFactor, HeadBobAmplitude);
        }

        else
        {
            CameraVisuals.ResetHeadBob(ResetHeadBobSpeed);
        }

        CameraVisuals.StabilizeCamera();

        HandleCameraVisuals();
    }

    protected virtual bool CanHeadBob()
    {
        return ((bIsGrounded && !bSliding) || bWallLeft || bWallRight) && new Vector3(Velocity.x, 0, Velocity.z).magnitude > 2.5f;
    }

    protected virtual void HandleCameraVisuals()
    {
        CameraVisuals.ChangeFOVSmooth(Mathf.Clamp(new Vector3(Velocity.x, 0, Velocity.z).magnitude, 0, 45) * VelocityFOVFactor, VelocityFOVSpeed);
        CameraVisuals.TiltY(Velocity.y, CameraTiltYSpeed);

        if (bSliding)
        {
            CameraVisuals.OffsetSmoothPosRot(SlideCameraOffset, SlideCameraSpeed, SlideRotationOffset, SlideRotationOffset / SlideCameraDuration);
        }

        else
        {
            CameraVisuals.ResetPositionSmoothPosRot(SlideCameraResetSpeed, SlideRotationOffset / SlideCameraResetDuration);

            if (bWallLeft)
            {
                CameraVisuals.Tilt(-7);

                return;
            }

            if (bWallRight)
            {
                CameraVisuals.Tilt(7);

                return;
            }

            if (CurrentInput.A && !CurrentInput.D)
            {
                CameraVisuals.Tilt(1.5f);

                return;
            }

            if (CurrentInput.D)
            {
                CameraVisuals.Tilt(-1.5f);

                return;
            }
        }

        CameraVisuals.Tilt(0);
    }

    protected virtual void HandleOtherPlayerVisuals()
    {

    }

    protected virtual void AbilityTick()
    {

    }

    protected virtual void MovePlayer()
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

        NormalMovement();
    }

    protected virtual void NormalMovement()
    {
        Vector3 Delta;

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

        else
        {
            if (IsOwner)
            {
                float SlideChargeTime = (float)(CurrentTimeStamp - TimeStartSlideGroundPound) / SlideChargeJumpTime;

                if (SlideChargeTime > 1)
                {
                    SlideChargeJumpProgressBar.UpdateProgressBar(1);
                }

                else
                {
                    SlideChargeJumpProgressBar.UpdateProgressBar(SlideChargeTime);
                }
            }

            if (!CurrentInput.CTRL || Velocity.magnitude < SlideMinSpeed)
            {
                ExitSlide();
            }
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

                    if (CurrentTimeStamp - TimeStartSlideGroundPound < SlideChargeJumpTime)
                    {
                        JumpVel = (Vector3.up * 2.5f + SlideDirection).normalized * SlideJumpForce;

                        Velocity = Vector3.ClampMagnitude(Velocity, SlideMoveSpeed * 1.5f);

                        if (IsOwner)
                        {
                            JumpAudio.Play();
                        }
                    }

                    else
                    {
                        JumpVel = SlideChargeJumpMagnitude * JumpForce * Vector3.up;

                        if (IsOwner)
                        {
                            BoostedJumpAudio.Play();
                        }
                    }
                }

                else
                {
                    JumpVel = Vector3.up * JumpForce;

                    if (IsOwner)
                    {
                        JumpAudio.Play();
                    }
                }
            }

            if (bSliding)
            {
                float Mag = Velocity.magnitude;

                if (Mag < SlideMoveSpeed)
                {
                    Delta = ((SlideDirection * SlideMoveSpeed)) * DeltaTime;
                }

                else
                {
                    Delta = ((SlideDirection * Mag) * (1 - SlideFriction * DeltaTime)) * DeltaTime;
                }
            }

            else
            {
                Delta = (Velocity * (1 - GroundFriction * DeltaTime) + ((1 + GroundFriction) * DeltaTime * (MoveDirection * WalkMoveSpeed)) + JumpVel) * DeltaTime;
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

                    if (IsOwner)
                    {
                        JumpAudio.Play();
                    }
                }

                Delta = (Velocity * (1 - WallRunFriction * DeltaTime) + ((1 + WallRunFriction) * DeltaTime * (WallForward * WallRunSpeed)) + JumpVel) * DeltaTime;
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

                    if (IsOwner)
                    {
                        JumpAudio.Play();
                    }
                }

                Delta = (Velocity * (1 - WallRunFriction * DeltaTime) + ((1 + WallRunFriction) * DeltaTime * (WallForward * WallRunSpeed)) + JumpVel) * DeltaTime;
            }

            else
            {
                if (bGroundPound)
                {
                    Delta = Velocity * DeltaTime;
                }

                else
                {
                    Delta = (Velocity * (1 - AirFriction * DeltaTime) + ((1 + AirFriction) * DeltaTime * (MoveDirection * WalkMoveSpeed)) + (Gravity * Vector3.down)) * DeltaTime;
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

    protected virtual void EnterGrounded()
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
                    JumpVel = (MaxGroundPoundJumpForce + 0.25f) * JumpForce * Vector3.up;
                }

                else
                {
                    JumpVel = (1.25f + (CurrentTimeStamp - TimeStartSlideGroundPound) / MaxGroundPoundChargeJumpTime * (MaxGroundPoundJumpForce - 1)) * JumpForce * Vector3.up;
                }

                if (IsOwner)
                {
                    BoostedJumpAudio.Play();
                }
            }
        }

        else if (Time.time - LastTimePlayedLandAudio >= LandAudioCooldown)
        {
            LastTimePlayedLandAudio = Time.time;
            LandAudio.Play();
        }
    }

    protected virtual void EnterSlide()
    {
        bTrySlideGroundPound = false;
        TimeStartSlideGroundPound = CurrentTimeStamp;
        bSliding = true;

        if (MoveDirection == Vector3.zero)
        {
            Vector3 VelocityXY = new Vector3(Velocity.x, 0, Velocity.z);

            if (VelocityXY.magnitude < 1)
            {
                SlideDirection = ForwardRotation * Vector3.forward;
            }

            else
            {
                SlideDirection = VelocityXY.normalized;
            }
        }

        else
        {
            SlideDirection = MoveDirection;
        }

        Velocity = new Vector3(Velocity.x, Velocity.y / 2, Velocity.z);

        if(Velocity.magnitude < SlideMoveSpeed)
        {
            Velocity = Velocity.normalized * SlideMoveSpeed * 1.5F;
        }

        else
        {
            Velocity += Velocity.normalized * SlideMoveSpeed * 0.5f;
        }

        EnterSlideVisuals();
    }

    protected virtual void EnterSlideVisuals()
    {
        if (IsOwner)
        {
            SlideChargeJumpUIObject.SetActive(true);
            SlideChargeJumpProgressBar.UpdateProgressBar(0);
        }

        SlideParticles.transform.position = SelfTransform.position + MoveDirection * 0.5f + Vector3.down * SlideParticleOffset;
        SlideParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position + Vector3.down * SlideParticleOffset - SlideParticles.transform.position), Vector3.up);
        SlideParticles.Play();
        SlideSmoke.Play();

        SlideSound.Play();
    }

    protected virtual void ExitSlide()
    {
        if (!bSliding)
        {
            return;
        }

        bSliding = false;
        LastTimeSlide = CurrentTimeStamp;

        ExitSlideVisuals();
    }

    protected virtual void ExitSlideVisuals()
    {
        if (IsOwner)
        {
            SlideChargeJumpUIObject.SetActive(false);
        }

        if (Time.time - LastTimePlayedSlideExitAudio >= LandAudioCooldown)
        {
            LastTimePlayedSlideExitAudio = Time.time;
            SlideExitSound.Play();
        }

        SlideParticles.Stop();
        SlideSmoke.Stop();
        SlideSound.Stop();
    }

    protected virtual void EnterGroundPound()
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

    protected virtual void ExitGroundPound(bool bHitGround)
    {
        if (bGroundPound)
        {
            bGroundPound = false;

            if (bHitGround)
            {
                Velocity.x = 0;
                Velocity.z = 0;

                if (IsServer)
                {
                    int NumHits = Physics.OverlapSphereNonAlloc(SelfTransform.position, GroundPoundRadius, GroundPoundHits, PlayerLayer);

                    for (int i = 0; i < NumHits; i++)
                    {
                        GroundPoundHits[i].GetComponent<BasePlayerManager>().Damage(Player.GetTeam(), GroundPoundDamage);
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

    protected virtual bool CheckForRightWall()
    {
        return bWallRight = Physics.Raycast(SelfTransform.position, ForwardRotation * Vector3.right, out RightWallHit, WallRunCheckForWallRange, WhatIsGround);
    }

    protected virtual bool CheckForLeftWall()
    {
        return bWallLeft = Physics.Raycast(SelfTransform.position, -(ForwardRotation * Vector3.right), out LeftWallHit, WallRunCheckForWallRange, WhatIsGround);
    }

    protected virtual void SafeMovePlayer(Vector3 delta)
    {
        SelfTransform.position += CollideAndSlide(SelfTransform.position, delta, 0);
    }

    protected virtual Vector3 CollideAndSlide(Vector3 Pos, Vector3 Vel, int depth)
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
            PhysicsLayerMask
            ))
        {

            Vector3 SnapToSurface = Vector3.ClampMagnitude(Vel.normalized * (hit.distance + SkinWidth / Mathf.Cos(Vector3.Angle(Vel, hit.normal) * Mathf.PI / 180)), 0.5f);

            return SnapToSurface + CollideAndSlide(Pos + SnapToSurface, Vector3.ProjectOnPlane(Vel - SnapToSurface, hit.normal), depth + 1);
        }

        int PenetrationAttempts = 1;

        while (Physics.OverlapCapsuleNonAlloc(
            Pos + ColliderOffset1,
            Pos + ColliderOffset2,
            CollidingRadius,
            Penetrations,
            PhysicsLayerMask
            )
            == 1 && PenetrationAttempts <= MaxResolvePenetrationAttempts + 1)
        {
            Vector3 ResolvePenetration = PenetrationAttempts * PenetrationAttempts * ResolvePenetrationDistance * (Pos - Penetrations[0].ClosestPoint(Pos)).normalized;

            if (ResolvePenetration == Vector3.zero)
            {
                ResolvePenetration = PenetrationAttempts * PenetrationAttempts * ResolvePenetrationDistance * (Pos - Penetrations[0].bounds.center).normalized;
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
        if ((clientpos - serverpos).magnitude <= CorrectionDistance)
        {
            SelfTransform.position = clientpos;

            return;
        }

        TryClientCorrection();

        print("Client Error Detected" + CurrentTimeStamp);
    }

    public void TryClientCorrection()
    {
        if(Time.time - LastTimeSentCorrection < MinTimeBetweenCorrections / 10)
        {
            return;
        }

        LastTimeSentCorrection = Time.time;

        SendClientCorrection();
    }

    public virtual void SendClientCorrection() { }

    protected virtual void SetToServerState() { }

    private void ReplayMovesAfterCorrection()
    {
        if (bRewindingClientCorrection)
        {
            bRewindingClientCorrection = false;

            SetToServerState();
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

    protected virtual void AfterCorrectionReceived(int replaytimestamp)
    {
        ReplayMoves = true;
        SimulateTimeStamp = replaytimestamp;

        bSmoothingCorrection = true;
        StartSmoothingCorrectionTime = CurrentTimeStamp;
        StartCorrectionPosition = SelfTransform.position;
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

    protected virtual void HandleInputs(ref Inputs input)
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

        if (!IsOwner)
        {
            if (!bSliding)
            {
                TPOrientation.rotation = ForwardRotation;
            }

            else
            {
                TPOrientation.rotation = Quaternion.LookRotation(Velocity);
            }
        }

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

        else if (!input.SpaceBar)
        {
            bTryJump = false;
        }

        bWasSpace = input.SpaceBar;
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void SendInputsServerRpc(Inputs input)
    {
        if (input.TimeStamp > CurrentTimeStamp)
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
        FPOrientation.rotation = rotation;

        if (issliding && !bSliding)
        {
            EnterSlide();
        }

        else if (!issliding && bSliding)
        {
            ExitSlide();
        }

        if (!bSliding)
        {
            float a = Mathf.Sqrt((rotation.w * rotation.w) + (rotation.y * rotation.y));
            ForwardRotation = new Quaternion(0, rotation.y / a, 0, rotation.w / a);

            TPOrientation.rotation = ForwardRotation;
        }

        else
        {
            TPOrientation.rotation = Quaternion.LookRotation(Velocity);
        }

        if (isgroundpounding && !bGroundPound)
        {
            bGroundPound = true;

            GroundPoundTrails.Play();
        }

        else if (!isgroundpounding && bGroundPound)
        {
            ExitGroundPound(true);
        }

        OnBaseReplicateMovement();
    }

    protected virtual void OnBaseReplicateMovement()
    {

    }

    public void ChangeVelocity(Vector3 NewVel, bool bExternalSource)
    {
        ExitSlide();
        ExitGroundPound(false);

        Velocity = NewVel;

        if (!IsServer || IsOwner || bNoMovement || !bExternalSource)
        {
            return;
        }

        TryClientCorrection();
    }

    public void AddVelocity(Vector3 Impulse, bool bExternalSource)
    {
        ExitSlide();
        ExitGroundPound(false);

        Velocity += Impulse;

        if (!IsServer || IsOwner || bNoMovement || !bExternalSource)
        {
            return;
        }
        
        TryClientCorrection();
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

        ServerExternalMoveState.Position = pos;
        ServerExternalMoveState.Velocity = vel;
    }

    private void SetToOnScoreState()
    {
        ResetMovement();

        SelfTransform.position = ServerExternalMoveState.Position;
        Velocity = ServerExternalMoveState.Velocity;
    }

    public virtual void ResetMovement()
    {
        ExitSlide();
        ExitGroundPound(false);

        Velocity = Vector3.zero;
    }

    public void UpdateIgnoreOwnerRPCParams(ClientRpcParams newIgnoreOwnerRPCParams)
    {
        IgnoreOwnerRPCParams = newIgnoreOwnerRPCParams;
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

    public Quaternion GetForwardRotation()
    {
        return ForwardRotation;
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

    public ulong GetOwnerID()
    {
        return OwnerClientId;
    }

    public BasePlayerManager GetPlayer()
    {
        return Player;
    }

    public bool GetIsDead()
    {
        return Player.GetIsDead();
    }
}