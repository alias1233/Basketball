using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public enum SpellList
{
    Fireball,
    Updraft,
    MeteorStrike,
    LaserBeam,
    Slash,
    Tornado
}


public class Spellbook : NetworkBehaviour
{
    private Transform SelfTransform;

    public BaseCharacterComponents components;

    private BasePlayerManager Player;
    [HideInInspector]
    public BaseCharacterMovement PlayerMovementComponent;
    private Transform TPOrientation;
    private Transform FPOrientation;

    private GameObject CameraObject;
    private CameraScript camerascript;
    private Camera FPCamera;

    public Transform AimPoint;

    private int CurrentTimeStamp;

    private NetworkRole LocalRole;
    private ClientRpcParams OwningClientID;
    private ClientRpcParams IgnoreOwnerRPCParams;

    public LayerMask ObjectLayer;
    public LayerMask PlayerLayer;

    private RaycastHit[] Hits = new RaycastHit[5];

    public GameObject SpellbookObject;
    private Vector3 InitialRotation;

    private bool bSpellActive;
    private SpellList CurrentSpell;
    private GameObject CurrentSpellVisuals;

    public Color ActiveColor;
    private ColorBlock SpellbookOriginalColor;
    private ColorBlock SpellbookActiveColor;

    public LayerMask SpellbookButtonsMask;

    public Button SpellbookButton1;
    public Button SpellbookButton2;
    public Button SpellbookButton3;
    public Button SpellbookButton4;
    public Button SpellbookButton5;
    public Button SpellbookButton6;
    public Button SpellbookButton7;
    public Button SpellbookButton8;
    public Button SpellbookButton9;

    bool SpellInput1;
    bool SpellInput2;
    bool SpellInput3;
    bool SpellInput4;
    bool SpellInput5;
    bool SpellInput6;
    bool SpellInput7;
    bool SpellInput8;
    bool SpellInput9;

    private bool bLeftMouseDown;

    public ProgressBar ManaBar;
    public float MaxMana = 100;
    public float ManaRegenRate = 0.02f;
    private float Mana;

    public GameObject FireballVisuals;
    public GameObject UpdraftVisuals;
    public GameObject MeteorStrikeVisuals;
    public GameObject LaserBeamVisuals;
    public GameObject SlashVisuals;
    public GameObject TornadoVisuals;

    public float FireballManaCost;
    public float UpdraftManaCost;
    public float MeteorStrikeManaCost;
    public float LaserBeamManaCost;
    public float SlashManaCost;
    public float TornadoManaCost;

    public NetworkObjectPool FireballPool;

    public ParticleSystem UpdraftParticles;
    public AudioSource UpdraftSound;
    public float UpdraftImpulse = 25;

    public NetworkObjectPool MeteorPool;
    public float MeteorOffset;
    public float MeteorRange = 30;

    public NetworkObjectPool LaserBeamPool;

    public ParticleSystem SlashParticle;
    public int SlashCooldown;
    private int LastTimeSlash;
    private int SlashAmount;
    public int SlashRadius;
    public int SlashRange;
    public int SlashDamage;
    public AudioSource SlashSFX1; 
    public AudioSource SlashSFX2;
    public AudioSource SlashSFX3;

    public NetworkObjectPool TornadoPool;

    private void Awake()
    {
        SelfTransform = transform;

        Player = GetComponent<BasePlayerManager>();
        PlayerMovementComponent = GetComponent<BaseCharacterMovement>();
        TPOrientation = components.TPOrientation;
        FPOrientation = components.FPOrientation;
        camerascript = components.camerascript;
        CameraObject = components.FPPlayerCamera;
        FPCamera = components.FPCamera;

        CurrentSpellVisuals = FireballVisuals;

        Mana = MaxMana;
    }

    // Start is called before the first frame update
    void Start()
    {
        SpellbookOriginalColor = SpellbookButton1.colors;
        SpellbookActiveColor = SpellbookButton1.colors;
        SpellbookActiveColor.normalColor = ActiveColor;
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        if(Input.GetKeyDown(KeyCode.Mouse0))
        {
            bLeftMouseDown = true;
        }

        if (SpellbookObject.activeSelf)
        {
            SpellbookObject.transform.position = CameraObject.transform.position;

            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                AttemptSpell();
            }

            if (Input.GetKey(KeyCode.Mouse0))
            {
                RaycastHit hit;

                if (Physics.Raycast(CameraObject.transform.position, CameraObject.transform.forward, out hit, 2, SpellbookButtonsMask))
                {
                    ActivateButton(hit.transform.GetComponent<SpellbookButtons>().SpellbookNum);
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            if (!SpellbookObject.activeSelf)
            {
                ActivateSpellbool();
            }

            else
            {
                DeactivateSpellbook();
            }
        }
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
        Mana += ManaRegenRate;
        
        if(Mana > MaxMana)
        {
            Mana = MaxMana;
        }

        ManaBar.UpdateProgressBar(Mana / MaxMana);

        if (bSpellActive && !SpellbookObject.activeSelf && Input.GetKey(KeyCode.Mouse0))
        {
            switch (CurrentSpell)
            {
                case SpellList.Fireball:
                    Fireball(PlayerMovementComponent.GetRotation() * Vector3.forward);
                    return;

                case SpellList.MeteorStrike:
                    MeteorStrike(PlayerMovementComponent.GetRotation() * Vector3.forward);
                    return;

                case SpellList.Updraft:
                    Updraft();
                    return;

                case SpellList.LaserBeam:
                    LaserBeam(PlayerMovementComponent.GetRotation() * Vector3.forward);
                    return;

                case SpellList.Tornado:
                    Tornado(PlayerMovementComponent.GetRotation() * Vector3.forward);
                    return;
            }
        }

        if(bLeftMouseDown)
        {
            bLeftMouseDown = false;

            if (bSpellActive && !SpellbookObject.activeSelf)
            {
                switch (CurrentSpell)
                {
                    case SpellList.Slash:
                        Slash(PlayerMovementComponent.GetRotation() * Vector3.forward);
                        return;
                }
            }
        }
    }

    protected virtual void ServerTickForOtherPlayers()
    {

    }

    protected virtual void ClientOwnerTick()
    {
        Mana += ManaRegenRate;

        if (Mana > MaxMana)
        {
            Mana = MaxMana;
        }

        ManaBar.UpdateProgressBar(Mana / MaxMana);

        if (bSpellActive && !SpellbookObject.activeSelf && Input.GetKey(KeyCode.Mouse0))
        {
            switch (CurrentSpell)
            {
                case SpellList.Fireball:
                    CastSpellWithRotationServerRpc(SpellList.Fireball, PlayerMovementComponent.GetRotation() * Vector3.forward);

                    bSpellActive = false;
                    CurrentSpellVisuals.SetActive(false);
                    return;

                case SpellList.MeteorStrike:
                    CastSpellWithRotationServerRpc(SpellList.MeteorStrike, PlayerMovementComponent.GetRotation() * Vector3.forward);

                    bSpellActive = false;
                    CurrentSpellVisuals.SetActive(false);
                    return;

                case SpellList.Updraft:
                    Updraft();
                    CastSpellServerRpc(SpellList.Updraft);
                    return;

                case SpellList.LaserBeam:
                    CastSpellWithRotationServerRpc(SpellList.LaserBeam, PlayerMovementComponent.GetRotation() * Vector3.forward);

                    bSpellActive = false;
                    CurrentSpellVisuals.SetActive(false);
                    return;

                case SpellList.Tornado:
                    CastSpellWithRotationServerRpc(SpellList.Tornado, PlayerMovementComponent.GetRotation() * Vector3.forward);

                    bSpellActive = false;
                    CurrentSpellVisuals.SetActive(false);
                    return;
            }
        }

        if (bLeftMouseDown)
        {
            bLeftMouseDown = false;

            if (bSpellActive && !SpellbookObject.activeSelf)
            {
                switch (CurrentSpell)
                {
                    case SpellList.Slash:
                        SlashSimulate();
                        return;
                }
            }
        }
    }

    protected virtual void ServerTickForAll()
    {

    }

    protected virtual void ClientProxyTick()
    {

    }

    public void AttemptSpell()
    {
        DeactivateSpellbook();

        if (SpellInput1 && SpellInput2 && SpellInput3
            && Mana >= FireballManaCost)
        {
            Mana -= FireballManaCost;
            bSpellActive = true;
            CurrentSpell = SpellList.Fireball;
            CurrentSpellVisuals.SetActive(false);
            CurrentSpellVisuals = FireballVisuals;
            CurrentSpellVisuals.SetActive(true);

            return;
        }

        if (SpellInput2 && SpellInput5 && SpellInput8
            && Mana >= UpdraftManaCost)
        {
            Mana -= UpdraftManaCost;
            bSpellActive = true;
            CurrentSpell = SpellList.Updraft;
            CurrentSpellVisuals.SetActive(false);
            CurrentSpellVisuals = UpdraftVisuals;
            CurrentSpellVisuals.SetActive(true);

            return;
        }

        if (SpellInput7 && SpellInput8 && SpellInput9
            && Mana >= MeteorStrikeManaCost)
        {
            Mana -= MeteorStrikeManaCost;
            bSpellActive = true;
            CurrentSpell = SpellList.MeteorStrike;
            CurrentSpellVisuals.SetActive(false);
            CurrentSpellVisuals = MeteorStrikeVisuals;
            CurrentSpellVisuals.SetActive(true);

            return;
        }

        if (SpellInput4 && SpellInput5 && SpellInput6
            && Mana >= LaserBeamManaCost)
        {
            Mana -= LaserBeamManaCost;
            bSpellActive = true;
            CurrentSpell = SpellList.LaserBeam;
            CurrentSpellVisuals.SetActive(false);
            CurrentSpellVisuals = LaserBeamVisuals;
            CurrentSpellVisuals.SetActive(true);

            return;
        }

        if(SpellInput1 && SpellInput4 && SpellInput7
            && Mana >= SlashManaCost)
        {
            Mana -= SlashManaCost;
            bSpellActive = true;
            CurrentSpell = SpellList.Slash;
            CurrentSpellVisuals.SetActive(false);
            CurrentSpellVisuals = SlashVisuals;
            CurrentSpellVisuals.SetActive(true);

            SlashAmount = 3;

            return;
        }

        if (SpellInput3 && SpellInput6 && SpellInput9
            && Mana >= TornadoManaCost)
        {
            Mana -= TornadoManaCost;
            bSpellActive = true;
            CurrentSpell = SpellList.Tornado;
            CurrentSpellVisuals.SetActive(false);
            CurrentSpellVisuals = TornadoVisuals;
            CurrentSpellVisuals.SetActive(true);

            return;
        }
    }

    [ServerRpc]
    private void CastSpellWithRotationServerRpc(SpellList AttemptingSpell, Vector3 Rot)
    {
        switch (AttemptingSpell)
        {
            case SpellList.Fireball:
                Fireball(Rot);
                break;

            case SpellList.MeteorStrike:
                MeteorStrike(Rot);
                break;

            case SpellList.LaserBeam:
                LaserBeam(Rot);
                break;

            case SpellList.Slash:
                Slash(Rot);
                break;

            case SpellList.Tornado:
                Tornado(Rot);
                break;
        }
    }

    [ServerRpc]
    private void CastSpellServerRpc(SpellList AttemptingSpell)
    {
        switch (AttemptingSpell)
        {
            case SpellList.Updraft:
                Updraft();
                break;
        }
    }

    [ClientRpc]
    private void ReplicateSpellWithRotationClientRpc(SpellList AttemptingSpell, Vector3 Rot, ClientRpcParams clientRpcParams = default)
    {
        switch (AttemptingSpell)
        {

        }
    }

    [ClientRpc]
    private void ReplicateSpellClientRpc(SpellList AttemptingSpell, ClientRpcParams clientRpcParams = default)
    {
        switch (AttemptingSpell)
        {
            case SpellList.Updraft:
                UpdraftSimulate();
                break;

            case SpellList.Slash:
                SlashSimulate();
                break;
        }
    }

    private void Fireball(Vector3 Rot)
    {
        bSpellActive = false;
        CurrentSpellVisuals.SetActive(false);

        GameObject obj = FireballPool.GetPooledObject();

        if (obj != null)
        {
            RocketScript rocket = obj.GetComponent<RocketScript>();
            rocket.InitNoRot(Player.GetTeam(), AimPoint.position, Rot);
            rocket.Spawn();
        }
    }

    private void MeteorStrike(Vector3 Rot)
    {
        bSpellActive = false;
        CurrentSpellVisuals.SetActive(false);

        RaycastHit hit;

        if (Physics.Raycast(AimPoint.position, Rot, out hit, MeteorRange, ObjectLayer))
        {
            GameObject obj = MeteorPool.GetPooledObject();

            if (obj != null)
            {
                MeteorScript meteor = obj.GetComponent<MeteorScript>();
                meteor.InitStationary(Player.GetTeam(), hit.point + Vector3.up * MeteorOffset);
                meteor.Spawn();
            }
        }

        else if (Physics.Raycast(AimPoint.position + Rot * MeteorRange, Vector3.down, out hit, 50, ObjectLayer))
        {
            GameObject obj = MeteorPool.GetPooledObject();

            if (obj != null)
            {
                MeteorScript meteor = obj.GetComponent<MeteorScript>();
                meteor.InitStationary(Player.GetTeam(), hit.point + Vector3.up * MeteorOffset);
                meteor.Spawn();
            }
        }
    }

    private void Updraft()
    {
        bSpellActive = false;
        CurrentSpellVisuals.SetActive(false);

        if(IsServer)
        {
            ReplicateSpellClientRpc(SpellList.Updraft, IgnoreOwnerRPCParams);
        }

        Vector3 Velocity = PlayerMovementComponent.GetVelocity();
        PlayerMovementComponent.ChangeVelocity(new Vector3(Velocity.x, UpdraftImpulse, Velocity.z), true);

        UpdraftParticles.Play();
        UpdraftSound.Play();
    }

    private void UpdraftSimulate()
    {
        UpdraftParticles.Play();
        UpdraftSound.Play();
    }

    private void LaserBeam(Vector3 Rot)
    {
        bSpellActive = false;
        CurrentSpellVisuals.SetActive(false);

        GameObject obj = LaserBeamPool.GetPooledObject();

        if (obj != null)
        {
            LaserBeamScript rocket = obj.GetComponent<LaserBeamScript>();
            rocket.Init(Player.GetTeam(), AimPoint.position + Rot, Rot);
            rocket.Spawn();
        }
    }

    private void Slash(Vector3 Rot)
    {
        if (CurrentTimeStamp - LastTimeSlash >= SlashCooldown)
        {
            LastTimeSlash = CurrentTimeStamp;

            SlashParticle.Play();
            int rand = Random.Range(1, 4);

            if (rand == 1)
            {
                SlashSFX1.Play();
            }

            else if (rand == 2)
            {
                SlashSFX2.Play();
            }

            else
            {
                SlashSFX3.Play();
            }

            ReplicateSpellClientRpc(SpellList.Slash, IgnoreOwnerRPCParams);

            int NumHits = Physics.SphereCastNonAlloc(AimPoint.position, SlashRadius, Rot, Hits, SlashRange, PlayerLayer);

            for (int i = 0; i < NumHits; i++)
            {
                if (Hits[i].transform.gameObject.TryGetComponent<BasePlayerManager>(out BasePlayerManager stats))
                {
                    stats.Damage(Player.GetTeam(), SlashDamage);
                }
            }

            SlashAmount--;

            if (SlashAmount <= 0)
            {
                bSpellActive = false;
                CurrentSpellVisuals.SetActive(false);
            }
        }
    }

    private void SlashSimulate()
    {
        if (CurrentTimeStamp - LastTimeSlash >= SlashCooldown)
        {
            LastTimeSlash = CurrentTimeStamp;

            SlashParticle.Play();
            int rand = Random.Range(1, 4);

            if (rand == 1)
            {
                SlashSFX1.Play();
            }

            else if (rand == 2)
            {
                SlashSFX2.Play();
            }

            else
            {
                SlashSFX3.Play();
            }

            CastSpellWithRotationServerRpc(SpellList.Slash, PlayerMovementComponent.GetRotation() * Vector3.forward);

            SlashAmount--;

            if (SlashAmount <= 0)
            {
                bSpellActive = false;
                CurrentSpellVisuals.SetActive(false);
            }
        }
    }

    private void Tornado(Vector3 Rot)
    {
        bSpellActive = false;
        CurrentSpellVisuals.SetActive(false);

        GameObject obj = TornadoPool.GetPooledObject();

        if (obj != null)
        {
            BaseProjectile rocket = obj.GetComponent<BaseProjectile>();
            rocket.InitNoRot(Player.GetTeam(), AimPoint.position + Vector3.down * 2.5f - Rot * 2.5f, Rot);
            rocket.Spawn();
        }
    }

    protected void ActivateSpellbool()
    {
        SpellbookObject.SetActive(true);

        InitialRotation = PlayerMovementComponent.GetForwardRotation() * Vector3.forward;
        SpellbookObject.transform.position = CameraObject.transform.position;
        SpellbookObject.transform.LookAt(CameraObject.transform.position + InitialRotation);

        SpellInput1 = false;
        SpellInput2 = false;
        SpellInput3 = false;
        SpellInput4 = false;
        SpellInput5 = false;
        SpellInput6 = false;
        SpellInput7 = false;
        SpellInput8 = false;
        SpellInput9 = false;

        SpellbookButton1.colors = SpellbookOriginalColor;
        SpellbookButton2.colors = SpellbookOriginalColor;
        SpellbookButton3.colors = SpellbookOriginalColor;
        SpellbookButton4.colors = SpellbookOriginalColor;
        SpellbookButton5.colors = SpellbookOriginalColor;
        SpellbookButton6.colors = SpellbookOriginalColor;
        SpellbookButton7.colors = SpellbookOriginalColor;
        SpellbookButton8.colors = SpellbookOriginalColor;
        SpellbookButton9.colors = SpellbookOriginalColor;
    }

    protected void DeactivateSpellbook()
    {
        SpellbookObject.SetActive(false);
    }

    public void ActivateButton(int buttonnum)
    {
        switch (buttonnum)
        {
            case 1:
                SpellbookButton1.colors = SpellbookActiveColor;
                SpellInput1 = true;
                break;
            case 2:
                SpellbookButton2.colors = SpellbookActiveColor;
                SpellInput2 = true;
                break;
            case 3:
                SpellbookButton3.colors = SpellbookActiveColor;
                SpellInput3 = true;
                break;
            case 4:
                SpellbookButton4.colors = SpellbookActiveColor;
                SpellInput4 = true;
                break;
            case 5:
                SpellbookButton5.colors = SpellbookActiveColor;
                SpellInput5 = true;
                break;
            case 6:
                SpellbookButton6.colors = SpellbookActiveColor;
                SpellInput6 = true;
                break;
            case 7:
                SpellbookButton7.colors = SpellbookActiveColor;
                SpellInput7 = true;
                break;
            case 8:
                SpellbookButton8.colors = SpellbookActiveColor;
                SpellInput8 = true;
                break;
            case 9:
                SpellbookButton9.colors = SpellbookActiveColor;
                SpellInput9 = true;
                break;
        }
    }

    public void UpdateIgnoreOwnerRPCParams(ClientRpcParams newIgnoreOwnerRPCParams)
    {
        IgnoreOwnerRPCParams = newIgnoreOwnerRPCParams;
    }

    public bool GetIsSpellbookActive()
    {
        return SpellbookObject.activeSelf;
    }
}
