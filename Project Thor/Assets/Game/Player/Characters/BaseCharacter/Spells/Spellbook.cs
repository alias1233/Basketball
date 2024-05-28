using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI.Table;

public enum SpellList
{
    Fireball,
    Updraft,
    MeteorStrike,
    LaserBeam
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

    public GameObject FireballVisuals;
    public GameObject UpdraftVisuals;
    public GameObject MeteorStrikeVisuals;
    public GameObject LaserBeamVisuals;

    public NetworkObjectPool FireballPool;

    public ParticleSystem UpdraftParticles;
    public float UpdraftImpulse = 25;

    public NetworkObjectPool MeteorPool;
    public float MeteorOffset;
    public float MeteorRange = 30;

    public BigLaserScript BigLaser;
    public AudioSource LaserSound;
    public float LaserRange;
    public float LaserDamage;
    public float LaserRadius;

    private void Awake()
    {
        SelfTransform = transform;

        Player = GetComponent<BasePlayerManager>();
        PlayerMovementComponent = GetComponent<BaseCharacterMovement>();
        TPOrientation = components.TPOrientation;
        FPOrientation = components.FPOrientation;
        camerascript = components.camerascript;
        FPCamera = components.FPCamera;

        CurrentSpellVisuals = FireballVisuals;
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

        if (Input.GetKeyUp(KeyCode.Mouse0) && SpellbookObject.activeSelf)
        {
            AttemptSpell();
        }

        if (Input.GetKey(KeyCode.Mouse0) && SpellbookObject.activeSelf)
        {
            Ray ray = FPCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 5, SpellbookButtonsMask))
            {
                ActivateButton(hit.transform.GetComponent<SpellbookButtons>().SpellbookNum);
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
        if (bSpellActive && !SpellbookObject.activeSelf && Input.GetKey(KeyCode.Mouse0))
        {
            switch (CurrentSpell)
            {
                case SpellList.Fireball:
                    Fireball(PlayerMovementComponent.GetRotation() * Vector3.forward);
                    break;

                case SpellList.MeteorStrike:
                    MeteorStrike(PlayerMovementComponent.GetRotation() * Vector3.forward);
                    break;

                case SpellList.Updraft:
                    Updraft();
                    break;

                case SpellList.LaserBeam:
                    LaserBeam(PlayerMovementComponent.GetRotation() * Vector3.forward);
                    break;
            }
        }
    }

    protected virtual void ServerTickForOtherPlayers()
    {

    }

    protected virtual void ClientOwnerTick()
    {
        if (bSpellActive && !SpellbookObject.activeSelf && Input.GetKey(KeyCode.Mouse0))
        {
            switch (CurrentSpell)
            {
                case SpellList.Fireball:
                    CastSpellWithRotationServerRpc(SpellList.Fireball, PlayerMovementComponent.GetRotation() * Vector3.forward);

                    bSpellActive = false;
                    CurrentSpellVisuals.SetActive(false);
                    break;

                case SpellList.MeteorStrike:
                    CastSpellWithRotationServerRpc(SpellList.MeteorStrike, PlayerMovementComponent.GetRotation() * Vector3.forward);

                    bSpellActive = false;
                    CurrentSpellVisuals.SetActive(false);
                    break;

                case SpellList.Updraft:
                    Updraft();
                    CastSpellServerRpc(SpellList.Updraft);
                    break;

                case SpellList.LaserBeam:
                    CastSpellWithRotationServerRpc(SpellList.LaserBeam, PlayerMovementComponent.GetRotation() * Vector3.forward);
                    LaserBeamSimulate(PlayerMovementComponent.GetRotation() * Vector3.forward);
                    break;
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

        if (SpellInput1 && SpellInput2 && SpellInput3)
        {
            bSpellActive = true;
            CurrentSpell = SpellList.Fireball;
            CurrentSpellVisuals.SetActive(false);
            CurrentSpellVisuals = FireballVisuals;
            CurrentSpellVisuals.SetActive(true);

            return;
        }

        if (SpellInput2 && SpellInput5 && SpellInput8)
        {
            bSpellActive = true;
            CurrentSpell = SpellList.Updraft;
            CurrentSpellVisuals.SetActive(false);
            CurrentSpellVisuals = UpdraftVisuals;
            CurrentSpellVisuals.SetActive(true);

            return;
        }

        if (SpellInput7 && SpellInput8 && SpellInput9)
        {
            bSpellActive = true;
            CurrentSpell = SpellList.MeteorStrike;
            CurrentSpellVisuals.SetActive(false);
            CurrentSpellVisuals = MeteorStrikeVisuals;
            CurrentSpellVisuals.SetActive(true);

            return;
        }

        if (SpellInput4 && SpellInput5 && SpellInput6)
        {
            bSpellActive = true;
            CurrentSpell = SpellList.LaserBeam;
            CurrentSpellVisuals.SetActive(false);
            CurrentSpellVisuals = LaserBeamVisuals;
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
            case SpellList.LaserBeam:
                LaserBeamSimulate(Rot);
                break;
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
    }

    private void UpdraftSimulate()
    {
        UpdraftParticles.Play();
    }

    private void LaserBeam(Vector3 Rot)
    {
        bSpellActive = false;
        CurrentSpellVisuals.SetActive(false);

        ReplicateSpellWithRotationClientRpc(SpellList.LaserBeam, Rot, IgnoreOwnerRPCParams);

        RaycastHit colliderInfo2;

        if (Physics.Raycast(AimPoint.position, PlayerMovementComponent.GetRotation() * Vector3.forward, out colliderInfo2, LaserRange, ObjectLayer))
        {
            BigLaser.ShootLaser(colliderInfo2.distance / 2, AimPoint.forward);

            int NumHits = Physics.SphereCastNonAlloc(AimPoint.position, LaserRadius, Rot, Hits, colliderInfo2.distance, PlayerLayer);

            for (int i = 0; i < NumHits; i++)
            {
                if (Hits[i].transform.gameObject.TryGetComponent<BasePlayerManager>(out BasePlayerManager stats))
                {
                    stats.Damage(Player.GetTeam(), LaserDamage);
                }
            }
        }

        else
        {
            BigLaser.ShootLaser(LaserRange / 2, AimPoint.forward);

            int NumHits = Physics.SphereCastNonAlloc(AimPoint.position, LaserRadius, Rot, Hits, LaserRange, PlayerLayer);

            for (int i = 0; i < NumHits; i++)
            {
                if (Hits[i].transform.gameObject.TryGetComponent<BasePlayerManager>(out BasePlayerManager stats))
                {
                    stats.Damage(Player.GetTeam(), LaserDamage);
                }
            }
        }

        LaserSound.Play();
    }

    private void LaserBeamSimulate(Vector3 Rot)
    {
        bSpellActive = false;
        CurrentSpellVisuals.SetActive(false);

        RaycastHit colliderInfo2;

        if (Physics.Raycast(AimPoint.position, Rot, out colliderInfo2, LaserRange, ObjectLayer))
        {
            BigLaser.ShootLaser(colliderInfo2.distance / 2, AimPoint.forward);
        }

        else
        {
            BigLaser.ShootLaser(LaserRange / 2, AimPoint.forward);
        }

        LaserSound.Play();
    }

    protected void ActivateSpellbool()
    {
        SpellbookObject.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        camerascript.enabled = false;

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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        camerascript.enabled = true;
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
