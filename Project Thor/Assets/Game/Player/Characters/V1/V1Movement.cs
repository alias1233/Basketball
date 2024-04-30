using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class V1Movement : BaseCharacterMovement
{
    private ClientCorrection ServerState;

    [Header("// Abilities //")]

    public float FlyMoveSpeed = 10;
    public float FlySprintSpeed = 15;
    public float FlyFriction = 5;
    public float FlyGravityFactor = 0.1f;
    public float FlyInputInfluence = 0.5f;
    public float FlyInitialVelocityFactor;
    public float AfterFlyVelocityFactor = 2.5f;
    public float AfterFlyUpFactor;

    public int DashDuration;
    public int DashCooldown;
    public float DashSpeed;
    public float AfterDashVelocityMagnitude;

    public Transform GrappleShootLocationTransform;
    public Transform GrappleLineConnectedTransform;
    public NetworkObjectPool GrapplePool;

    public int GrappleDuration;
    public int GrappleShootCooldown;
    public float GrappleSpeed;
    public int MinGrappleTimeBeforeJumping;

    private int GrappleShootTime = -9999;

    public int FlyCooldown;
    public int FlyDuration;

    /*
    * 
    * Variables That Need To Be Sent For Client Corrections
    * 
    */

    private bool bDashing;
    private int StartDashTime = -9999;
    private Vector3 DashingStartRotation;

    private bool bGrapple;
    private int GrappleStartTime;
    private Vector3 GrappleLocation;

    private bool bPrevR;
    private bool bFly;
    private int LastTimeFly = -9999;

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

    protected override void Awake()
    {
        base.Awake();

        FlightProgressBar = FlightProgressBarObject.GetComponent<ProgressBar>();
    }

    protected override void ReplicateMovement()
    {
        if (bFly)
        {
            ReplicateFlyClientRpc(SelfTransform.position, Velocity, Rotation, IgnoreOwnerRPCParams);

            return;
        }

        ReplicatePositionClientRpc(SelfTransform.position, Velocity, Rotation, bSliding, bGroundPound, IgnoreOwnerRPCParams);
    }

    protected override void SimulatedProxyTick()
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

        base.SimulatedProxyTick();
    }

    protected override void HandleCameraVisuals()
    {
        if (bFly)
        {
            if (MoveDirection == Vector3.zero || MoveDirection == Vector3.up)
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

        if(bDashing)
        {
            if (CurrentInput.A && !CurrentInput.D)
            {
                CameraVisuals.Tilt(6f);

                return;
            }

            if (CurrentInput.D)
            {
                CameraVisuals.Tilt(-6f);

                return;
            }
        }

        base.HandleCameraVisuals();
    }

    protected override void HandleOtherPlayerVisuals()
    {
        if (bFly)
        {
            if (MoveDirection == Vector3.zero || MoveDirection == Vector3.up)
            {
                TPOrientation.rotation = Quaternion.RotateTowards(TPOrientation.rotation, ForwardRotation, 4f);
            }

            else
            {
                TPOrientation.rotation = Quaternion.LookRotation(Velocity.normalized, Vector3.up) * Quaternion.AngleAxis(60f, Vector3.right);
            }
        }
    }

    protected override void AbilityTick()
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

            if (CurrentTimeStamp - LastTimeFly > FlyDuration || (CurrentInput.R && !bPrevR && CurrentTimeStamp - LastTimeFly > 25))
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
                SafeMovePlayer(DashSpeed * DeltaTime * DashingStartRotation);
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
            if (CurrentTimeStamp - GrappleStartTime <= GrappleDuration && (GrappleLocation - SelfTransform.position).magnitude > 4.5f)
            {
                Grapple();
            }

            else
            {
                ExitGrapple();
            }
        }
    }

    private void EnterFly()
    {
        bFly = true;
        LastTimeFly = CurrentTimeStamp;
        Velocity += Rotation * Vector3.forward * FlyInitialVelocityFactor;

        if(Player.GetIsHoldingBall())
        {
            Ball.Singleton.Detach();
        }

        ExitSlide();
        ExitGroundPound(false);
        bIsGrounded = false;

        EnterFlyVisuals();
    }

    private void EnterFlyVisuals()
    {
        if (IsOwner)
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

        Velocity = Velocity * AfterFlyVelocityFactor + Vector3.up * AfterFlyUpFactor;

        ExitFlyVisuals();
    }

    private void ExitFlyVisuals()
    {
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
        bIsGrounded = false;

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
            grapplinghook.OwningPlayerMovement = this;

            if (IsServer && !IsOwner)
            {
                grapplinghook.InitAndSimulateForward(Player.GetTeam(), GrappleShootLocationTransform.position, Rotation * Vector3.forward, Player.GetHalfRTTInTick());
            }

            else
            {
                grapplinghook.Init(Player.GetTeam(), GrappleShootLocationTransform.position, Rotation * Vector3.forward);
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
        bIsGrounded = false;

        bDashing = false;

        GrappleStartTime = CurrentTimeStamp;
        bGrapple = true;
        bNoMovement = true;

        GrappleLocation = HitPos;

        StartGrappleVisuals();

        if (IsServer)
        {
            SendClientCorrection();
        }
    }

    private void StartGrappleVisuals()
    {
        if (IsOwner)
        {
            FirstPersonDashParticles.transform.position = FPOrientation.position + (GrappleLocation - SelfTransform.position).normalized * 2;
            FirstPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - FirstPersonDashParticles.transform.position), Vector3.up);
            FirstPersonDashParticles.Play();
        }

        else
        {
            ThirdPersonDashParticles.transform.position = FPOrientation.position + (GrappleLocation - SelfTransform.position).normalized * 2;
            ThirdPersonDashParticles.transform.rotation = Quaternion.LookRotation((FPOrientation.position - ThirdPersonDashParticles.transform.position), Vector3.up);
            ThirdPersonDashParticles.Play();
        }

        DashTrails.emitting = false;

        GrappleHit.Play();
        GrappleLoop.Play();
    }

    private void Grapple()
    {
        SafeMovePlayer(DeltaTime * GrappleSpeed * (GrappleLocation - SelfTransform.position).normalized);

        if (bTryJump && CurrentTimeStamp - LastTimeJumped > JumpCooldown && CurrentTimeStamp - GrappleStartTime >= MinGrappleTimeBeforeJumping)
        {
            bTryJump = false;

            LastTimeJumped = CurrentTimeStamp;

            ExitGrapple();
        }
    }

    private void ExitGrapple()
    {
        bGrapple = false;
        bNoMovement = false;

        Velocity = Velocity * 0.25f + JumpForce * 1.2f * Vector3.up;

        bChangingVelocity = true;

        ExitGrappleVisuals();
    }

    private void ExitGrappleVisuals()
    {
        if (IsOwner)
        {
            FirstPersonDashParticles.Stop();
        }

        GrappleLoop.Stop();
    }

    protected override void NormalMovement()
    {
        if (bFly)
        {
            Vector3 Delta;

            if (CurrentInput.CTRL)
            {
                Delta = ((1 - FlyFriction * DeltaTime) * Velocity.magnitude * (Velocity.normalized + MoveDirection * FlyInputInfluence).normalized + ((1 + FlyFriction) * DeltaTime * (MoveDirection * FlySprintSpeed))) * DeltaTime;
            }

            else
            {
                Delta = ((1 - FlyFriction * DeltaTime) * Velocity.magnitude * (Velocity.normalized + MoveDirection * FlyInputInfluence).normalized + ((1 + FlyFriction) * DeltaTime * (MoveDirection * FlyMoveSpeed))) * DeltaTime;
            }

            float GravityFactor = Vector3.Dot(Delta, Vector3.down);

            if (GravityFactor < 0)
            {
                Delta += 0.5f * FlyGravityFactor * GravityFactor * Velocity.normalized;
            }

            else
            {
                Delta += FlyGravityFactor * GravityFactor * Velocity.normalized;
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

            return;
        }

        base.NormalMovement();
    }

    /*
    *
    * Client Corrections
    *
    */

    public override void SendClientCorrection()
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
    }

    protected override void SetToServerState()
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
            ExitSlideVisuals();
        }

        else if(!bSliding && ServerState.bSliding)
        {
            EnterSlideVisuals();
        }

        bSliding = ServerState.bSliding;

        SlideDirection = ServerState.SlideDirection;
        LastTimeSlide = ServerState.LastTimeSlide;
        bWasCTRL = ServerState.bWasCTRL;
        bTrySlideGroundPound = ServerState.bTrySlideGroundPound;
        TimeStartSlideGroundPound = ServerState.TimeStartSlideGroundPound;
        bGroundPound = ServerState.bGroundPound;

        if(bGrapple && !ServerState.bGrapple)
        {
            ExitGrappleVisuals();
        }

        else if(!bGrapple && ServerState.bGrapple)
        {
            StartGrappleVisuals();
        }

        bGrapple = ServerState.bGrapple;
        GrappleStartTime = ServerState.GrappleStartTime;
        GrappleLocation = ServerState.GrappleLocation;
        bPrevR = ServerState.bPrevR;

        if (bFly && !ServerState.bFly)
        {
            ExitFly();
        }

        else if (!bFly && ServerState.bFly)
        {
            EnterFly();
        }

        bFly = ServerState.bFly;

        LastTimeFly = ServerState.LastTimeFly;
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ClientCorrectionClientRpc(ClientCorrection Data, ClientRpcParams clientRpcParams = default)
    {
        bRewindingClientCorrection = true;
        AfterCorrectionReceived(Data.TimeStamp);
        ServerState = Data;
    }

    /*
    *
    * Inputs
    *
    */

    protected override void HandleInputs(ref Inputs input)
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

            if(input.SpaceBar)
            {
                MoveDirection += Vector3.up;
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
                if(!bSliding)
                {
                    TPOrientation.rotation = ForwardRotation;
                }

                else
                {
                    TPOrientation.rotation = Quaternion.LookRotation(Velocity);
                }
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

        if(!bSliding)
        {
            float a = Mathf.Sqrt((rotation.w * rotation.w) + (rotation.y * rotation.y));
            ForwardRotation = new Quaternion(0, rotation.y / a, 0, rotation.w / a);

            TPOrientation.rotation = ForwardRotation;
        }

        else
        {
            TPOrientation.rotation = Quaternion.LookRotation(Velocity);
        }

        if(isgroundpounding && !bGroundPound)
        {
            bGroundPound = true;

            GroundPoundTrails.Play();
        }

        else if(!isgroundpounding && bGroundPound)
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

        if (Velocity.magnitude < 2.5f || Vector3.Dot(Velocity, Vector3.up) > 0.9f)
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

    public override void ResetMovement()
    {
        ExitGrapple();
        ExitDash();
        ExitFly();

        base.ResetMovement();
    }

    public Vector3 GetGrappleShootStartLocation()
    {
        return GrappleLineConnectedTransform.position;
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
}
