using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Fist : NetworkBehaviour
{
    private BasePlayerManager Player;
    [HideInInspector]
    public BaseCharacterMovement PlayerMovementComponent;
    private Spellbook Spells;
    public Transform TPOrientation;
    public Transform FPOrientation;

    public Transform AimPoint;

    private int CurrentTimeStamp;

    private NetworkRole LocalRole;
    private ClientRpcParams OwningClientID;
    private ClientRpcParams IgnoreOwnerRPCParams;

    public float SendInputCooldown = 0.1f;
    private float LastTimeSentInputs;

    private Dictionary<int, WeaponInputs> InputsDictionary = new Dictionary<int, WeaponInputs>();
    private WeaponInputs CurrentInput;

    public int RadiusOfRewindCheck;

    [Header("Melee")]

    public GameObject FistObject;
    public Transform FistParentTransform;
    public Transform FistParentParentTransform;

    public GameObject FistChargeBar;
    private ProgressBar ThrowChargeBar;

    public LayerMask PlayerAndBallLayer;

    [SerializeField]
    private MeleeAnimScript meleeanimation;

    public AudioSource PunchSwooshSound;
    public AudioSource PunchHitSound;

    public int MeleeCooldown;
    public int MeleeRange;
    public int MeleeDamage;

    private int LastTimeMelee;

    bool bIsCharging;
    int ChargingStartTime;

    public int MaxChargingTime = 50;

    public int ThrowForce;

    public float Radius;

    private RaycastHit[] Hits = new RaycastHit[5];

    private Vector3 FistOriginalPosition;
    private Quaternion FistOriginalRotation;

    [Header("Other")]

    public LayerMask ObjectLayer;

    private List<BasePlayerManager> RewindedPlayerList = new List<BasePlayerManager>();

    private RaycastHit[] RewindHits = new RaycastHit[5];

    public bool bHoldingBall;

    private void Start()
    {
        Player = GetComponent<BasePlayerManager>();
        PlayerMovementComponent = GetComponent<BaseCharacterMovement>();
        Spells = GetComponent<Spellbook>();
        ThrowChargeBar = FistChargeBar.GetComponent<ProgressBar>();

        LocalRole = Player.GetLocalRole();

        if (!IsOwner)
        {
            FistObject.SetActive(false);
        }

        FistOriginalPosition = FistObject.transform.localPosition;
        FistOriginalRotation = FistObject.transform.localRotation;
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
    }

    public void HostProxyTick(int timestamp)
    {
        CurrentTimeStamp = timestamp;

        ServerTickForOtherPlayers();
    }

    public void AutonomousProxyTick(int timestamp)
    {
        CurrentTimeStamp = timestamp;

        ClientOwnerTick();
    }

    void HostTick()
    {
        if (Input.GetKey(KeyCode.F))
        {
            if (CurrentTimeStamp - LastTimeMelee >= MeleeCooldown)
            {
                LastTimeMelee = CurrentTimeStamp;

                Melee();
            }
        }

        if (bHoldingBall)
        {
            if (Input.GetKey(KeyCode.Mouse0) && !Spells.GetIsSpellbookActive())
            {
                ChargeThrow();
            }

            else
            {
                ThrowBall();
            }

            return;
        }
    }

    void ServerTickForOtherPlayers()
    {
        if (InputsDictionary.TryGetValue(CurrentTimeStamp, out var input))
        {
            InputsDictionary.Remove(CurrentTimeStamp);
            CurrentInput = input;
        }

        if (CurrentInput.F)
        {
            if (CurrentTimeStamp - LastTimeMelee >= MeleeCooldown)
            {
                LastTimeMelee = CurrentTimeStamp;

                Melee();
            }
        }

        if (bHoldingBall)
        {
            if (CurrentInput.Mouse1)
            {
                ChargeThrow();
            }

            else
            {
                ThrowBall();
            }

            return;
        }
    }

    void ClientOwnerTick()
    {
        if (Time.time - LastTimeSentInputs >= SendInputCooldown 
            || CurrentInput.F != Input.GetKey(KeyCode.F)
            || CurrentInput.Mouse1 != Input.GetKey(KeyCode.Mouse0))
        {
            LastTimeSentInputs = Time.time;

            CurrentInput.TimeStamp = CurrentTimeStamp;
            CurrentInput.F = Input.GetKey(KeyCode.F);
            CurrentInput.Mouse1 = Input.GetKey(KeyCode.Mouse0) && !Spells.GetIsSpellbookActive();

            SendWeaponInputsServerRpc(CurrentInput);
        }

        if (CurrentInput.F)
        {
            if (CurrentTimeStamp - LastTimeMelee >= MeleeCooldown)
            {
                LastTimeMelee = CurrentTimeStamp;

                Melee();
            }
        }

        if (bHoldingBall)
        {
            if (CurrentInput.Mouse1)
            {
                ChargeThrow();
            }

            else
            {
                ThrowBall();
            }

            return;
        }
    }

    private void ChargeThrow()
    {
        if (bIsCharging)
        {
            if (IsOwner)
            {
                Ball.Singleton.SimulateThrow((
                    PlayerMovementComponent.GetRotation() * Vector3.forward + Vector3.up * 0.33f) *
                Mathf.Clamp(ThrowForce * (CurrentTimeStamp - ChargingStartTime) / MaxChargingTime, 5, ThrowForce));

                float chargingtime = CurrentTimeStamp - ChargingStartTime;

                if (chargingtime > MaxChargingTime)
                {
                    ThrowChargeBar.UpdateProgressBar(1);
                }

                else
                {
                    ThrowChargeBar.UpdateProgressBar(chargingtime / MaxChargingTime);
                }
            }

            return;
        }

        bIsCharging = true;
        ChargingStartTime = CurrentTimeStamp;

        if (IsOwner)
        {
            ThrowChargeBar.UpdateProgressBar(0);
            FistChargeBar.SetActive(true);

            Ball.Singleton.StartSimulatingThrow();
        }
    }

    private void ThrowBall()
    {
        if (!bIsCharging)
        {
            return;
        }

        bIsCharging = false;

        LastTimeMelee = CurrentTimeStamp - MeleeCooldown / 4;

        FistChargeBar.SetActive(false);

        MeleeVisual();

        if (bHoldingBall)
        {
            Ball.Singleton.Throw(
                (PlayerMovementComponent.GetRotation() * Vector3.forward + Vector3.up * 0.33f) *
                Mathf.Clamp(ThrowForce * (CurrentTimeStamp - ChargingStartTime) / MaxChargingTime, 5, ThrowForce)
                );
        }
    }

    public void EnterDunk()
    {
        if (bHoldingBall)
        {
            meleeanimation.EnterDunk();
        }
    }

    public void ExitDunk()
    {
        if (bHoldingBall)
        {
            meleeanimation.ExitDunk();
        }
    }

    private void Melee()
    {
        MeleeVisual();

        if (!IsServer)
        {
            int NumHits2 = Physics.SphereCastNonAlloc(new Ray(GetAimPointLocation(), PlayerMovementComponent.GetRotation() * Vector3.forward), Radius, Hits, MeleeRange, PlayerAndBallLayer);
            bool bHit2 = false;

            for (int i = 0; i < NumHits2; i++)
            {
                if (Hits[i].transform.gameObject.TryGetComponent<BasePlayerManager>(out BasePlayerManager stats))
                {
                    if (!stats.IsSameTeam(Player.GetTeam()))
                    {
                        bHit2 = true;
                    }
                }

                if (Hits[i].transform.gameObject.TryGetComponent<Ball>(out Ball ball))
                {
                    if (!ball.bAttached)
                    {
                        ball.Attach(Player);
                    }
                }
            }

            if (bHit2)
            {
                PunchHitSound.Play();
            }

            return;
        }

        ReplicateMeleeClientRpc(IgnoreOwnerRPCParams);

        Ray CenterRay = new Ray(GetAimPointLocation(), PlayerMovementComponent.GetRotation() * Vector3.forward);

        if (!IsOwner)
        {
            RewindPlayers(CenterRay, MeleeRange);
        }

        int NumHits = Physics.SphereCastNonAlloc(CenterRay, Radius, Hits, MeleeRange, PlayerAndBallLayer);
        bool bHit = false;

        for (int i = 0; i < NumHits; i++)
        {
            if (Hits[i].transform.gameObject.TryGetComponent<BasePlayerManager>(out BasePlayerManager stats))
            {
                if (stats.Damage(Player.GetTeam(), MeleeDamage))
                {
                    bHit = true;

                    if (stats.GetIsHoldingBall())
                    {
                        Ball.Singleton.Detach();
                        Ball.Singleton.Attach(Player);
                    }
                }
            }

            if (Hits[i].transform.gameObject.TryGetComponent<Ball>(out Ball ball))
            {
                if (!ball.bAttached)
                {
                    ball.Attach(Player);
                }
            }
        }

        if (bHit)
        {
            PunchHitSound.Play();
        }

        ResetRewindedPlayers();
    }

    private void MeleeVisual()
    {
        if (IsOwner)
        {
            meleeanimation.PunchAnim();
        }

        PunchSwooshSound.Play();
    }

    public bool GetIsChargingFist()
    {
        return bIsCharging;
    }

    public int GetFistStartChargeTime()
    {
        return ChargingStartTime;
    }

    public bool RewindPlayers(Ray ray, int range)
    {
        RewindedPlayerList.Clear();

        int NumHits = Physics.SphereCastNonAlloc(ray, Radius, RewindHits, range, PlayerAndBallLayer);
        bool bRewindedPlayers = false;

        for (int i = 0; i < NumHits; i++)
        {
            if (RewindHits[i].transform.gameObject.TryGetComponent<BasePlayerManager>(out BasePlayerManager rewind))
            {
                if (rewind.RewindToPosition(Player.GetTeam(), Player.GetPingInTick()))
                {
                    if (!Physics.Linecast(ray.origin, RewindHits[i].transform.position, ObjectLayer))
                    {
                        RewindedPlayerList.Add(rewind);

                        bRewindedPlayers = true;
                    }

                    else
                    {
                        rewind.ResetToOriginalPosition();
                    }
                }
            }
        }

        if (bRewindedPlayers)
        {
            Physics.SyncTransforms();

            return true;
        }

        return false;
    }

    public void ResetRewindedPlayers()
    {
        if (IsOwner)
        {
            return;
        }

        foreach (BasePlayerManager i in RewindedPlayerList)
        {
            i.ResetToOriginalPosition();
        }
    }

    [ServerRpc(Delivery = RpcDelivery.Unreliable)]
    public void SendWeaponInputsServerRpc(WeaponInputs input)
    {
        if (input.TimeStamp > CurrentTimeStamp)
        {
            InputsDictionary[input.TimeStamp] = input;
        }

        Player.CheckClientTimeError(input.TimeStamp);
    }

    [ClientRpc(Delivery = RpcDelivery.Unreliable)]
    public void ReplicateMeleeClientRpc(ClientRpcParams clientRpcParams = default)
    {
        MeleeVisual();
    }

    public void Attach()
    {
        if (!bHoldingBall)
        {
            bHoldingBall = true;
            meleeanimation.HoldBall();
            bIsCharging = false;

            if (IsOwner)
            {
                Ball.Singleton.DisableTrail();
            }
        }
    }

    public void Detach()
    {
        if (bHoldingBall)
        {
            bHoldingBall = false;
            FistChargeBar.SetActive(false);
            meleeanimation.UnholdBall();
            meleeanimation.ExitDunk();

            if (IsOwner)
            {
                Ball.Singleton.EnableTrail();
            }
        }
    }

    public void EnableFist()
    {
        FistObject.transform.localPosition = FistOriginalPosition;
        FistObject.transform.localRotation = FistOriginalRotation;
        FistObject.SetActive(true);
    }

    public void DisableFist()
    {
        FistObject.transform.localPosition = FistOriginalPosition;
        FistObject.transform.localRotation = FistOriginalRotation;
        FistObject.SetActive(false);
    }

    public void UpdateIgnoreOwnerRPCParams(ClientRpcParams newIgnoreOwnerRPCParams)
    {
        IgnoreOwnerRPCParams = newIgnoreOwnerRPCParams;
    }

    public Vector3 GetAimPointLocation()
    {
        return AimPoint.position;
    }
}
