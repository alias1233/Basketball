using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using static UnityEngine.UI.GridLayoutGroup;

public class PlayerMovement : NetworkBehaviour
{
    public Transform Orientation;
    public Rigidbody Rb;
    public CapsuleCollider Collider;
    public LayerMask layerMask;
    public LayerMask WhatIsGround;

    public float moveSpeed = 3f;
    public float JumpForce = 10f;
    public float GroundFriction;
    public float AirFriction;
    public float Gravity;

    public int ServerDelay = 5;

    private ClientRpcParams OwningClientID;
    private Dictionary<int, Inputs> InputsDictionary = new Dictionary<int, Inputs>();
    private Inputs CurrentInput;
    private int TimeStamp;

    private float DeltaTime;

    private int TotalTimes;
    private int TotalTimeDifference;
    private int LateInputsCount;
    private int EarlyInputsCount;
    private float LastTimeSentClientTimeCorrection;

    bool bAutonomousProxy;
    bool bHost;

    private Vector3 MoveDirection;
    private Vector3 Velocity;
    private bool bIsGrounded;

    private Bounds bounds;
    private int MaxBounces = 5;
    private float SkinWidth = 0.015f;
    private float MaxSlopeAngle = 55;

    // Start is called before the first frame update
    void Start()
    {
        Rb = GetComponent<Rigidbody>();

        bAutonomousProxy = IsClient && IsOwner;
        bHost = IsServer && IsOwner;

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
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        TimeStamp++;

        print(DeltaTime);

        DeltaTime = Time.fixedDeltaTime;

        if(bHost)
        {
            HostTick();

            ServerTickForAll();

            return;
        }

        if (bAutonomousProxy)
        {
            AutonomousProxyTick();

            return;
        }

        if (IsServer)
        {
            ServerTickForOtherPlayers();
        }
    }

    void HostTick()
    {
        CurrentInput = new Inputs(TimeStamp, Orientation.transform.rotation, Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.A), Input.GetKey(KeyCode.S), Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.Space));

        HandleInputs(CurrentInput);

        MovePlayer();
    }

    void ServerTickForOtherPlayers()
    {
        if(InputsDictionary.TryGetValue(TimeStamp, out Inputs inputs))
        {
            CurrentInput = inputs;
        }

        HandleInputs(CurrentInput);

        MovePlayer();
    }

    void AutonomousProxyTick()
    {
        CurrentInput = new Inputs(TimeStamp, Orientation.transform.rotation, Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.A), Input.GetKey(KeyCode.S), Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.Space));

        InputsDictionary.Add(TimeStamp, CurrentInput);

        SendInputsServerRpc(CurrentInput);

        HandleInputs(CurrentInput);

        MovePlayer();
    }

    void ServerTickForAll()
    {
        ReplicatePositionClientRpc(transform.position);
    }

    void MovePlayer()
    {
        bIsGrounded = Physics.Raycast(transform.position, Vector3.down, Collider.height * 0.5f + 0.15f, WhatIsGround);

        Debug.DrawLine(transform.position, transform.position + Vector3.down * (Collider.height * 0.5f + 0.1f));

        Vector3 JumpVel = Vector3.zero;
        Vector3 GravityVel = Gravity * Vector3.down;
        float frictionmultiplier = 1 - AirFriction * DeltaTime;
        Vector3 InputVel = (MoveDirection * moveSpeed) * (1 + AirFriction) * DeltaTime;

        if (bIsGrounded)
        {
            GravityVel = Vector3.zero;

            frictionmultiplier = 1 - GroundFriction * DeltaTime;

            InputVel = (MoveDirection * moveSpeed) * (1 + GroundFriction) * DeltaTime;

            Velocity.y = 0;

            if (CurrentInput.SpaceBar)
            {
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
            float Angle = Vector3.Angle(Vector3.up, hit.normal);

            if(SnapToSurface.magnitude <= SkinWidth)
            {
                SnapToSurface = Vector3.zero;
            }

            /*
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
            LateInputsCount++;
        }

        if (clienttime > TimeStamp + 2 * ServerDelay)
        {
            EarlyInputsCount++;
        }

        if (LateInputsCount >= 3 || EarlyInputsCount >= 3 || Time.time - LastTimeSentClientTimeCorrection > 7)
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

    void HandleInputs(Inputs input)
    {
        MoveDirection = Vector3.zero;

        Orientation.rotation = input.Rotation;

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

    [ClientRpc]
    public void SendClientTimeCorrectionClientRpc(int timediff, ClientRpcParams clientRpcParams = default)
    {
        TimeStamp += timediff;
    }

    [ClientRpc]
    public void ReplicatePositionClientRpc(Vector3 position)
    {
        if(bAutonomousProxy)
        {
            return;
        }
        
        transform.position = position;
    }

    [ServerRpc]
    public void SendInputsServerRpc(Inputs input)
    {
        InputsDictionary.Add(input.TimeStamp, input);

        CheckClientTimeError(input.TimeStamp);
    }
}
