using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;

public class BaseWeapon : MonoBehaviour
{
    [Header("Components")]

    [SerializeField]
    public WeaponManager Manager;
    [SerializeField]
    public PlayerMovement PlayerMovementComponent;

    [HideInInspector]
    public bool bIsOwner;
    [HideInInspector]
    public bool bIsServer;

    [SerializeField]
    public GameObject WeaponModel;
    private Transform WeaponModelTransform;
    [SerializeField]
    private Transform WeaponModelHolder;
    [SerializeField]
    public Transform MuzzlePoint;

    public AudioSource ShootSound1;
    public AudioSource ShootSound2;

    [Header("Attributes")]

    public int FireCooldown1;
    public int FireCooldown2;

    [HideInInspector]
    public int LastTimeShot1;
    [HideInInspector]
    public int LastTimeShot2;

    [Header("Other")]

    public LayerMask PlayerLayer;
    public LayerMask ObjectLayer;

    private List<PlayerManager> RewindedPlayerList = new List<PlayerManager>();

    private RaycastHit[] Hits = new RaycastHit[5];

    virtual public void Awake()
    {
        WeaponModelTransform = WeaponModel.transform;
    }

    virtual public void Start()
    {
        bIsOwner = Manager.GetIsOwner();
        bIsServer = Manager.GetHasAuthority();
    }

    public void ChangeActive(bool active)
    {
        if(active)
        {
            OnActivate();

            WeaponModel.SetActive(true);

            return;
        }

        OnDeactivate();

        WeaponModel.SetActive(false);
    }

    public bool RewindPlayers(Ray ray, int range)
    {
        RewindedPlayerList.Clear();

        int NumHits = Physics.SphereCastNonAlloc(ray, Manager.GetRadius(), Hits, range, PlayerLayer);
        bool bRewindedPlayers = false;

        for (int i = 0; i < NumHits; i++)
        {
            if (Hits[i].transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager rewind))
            {
                if (rewind.RewindToPosition(Manager.GetTeam(), Manager.GetPingInTick()))
                {
                    if (!Physics.Linecast(ray.origin, Hits[i].transform.position, ObjectLayer))
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

        if(bRewindedPlayers)
        {
            Physics.SyncTransforms();

            return true;
        }

        return false;
    }

    public void ResetRewindedPlayers()
    {
        if (Manager.GetIsOwner())
        {
            return;
        }

        foreach (PlayerManager i in RewindedPlayerList)
        {
            i.ResetToOriginalPosition();
        }
    }

    public void SetWeaponModelPos(Vector3 pos)
    {
        WeaponModelTransform.position = WeaponModelHolder.position + pos;
    }

    public virtual void OnActivate() { }

    public virtual void OnDeactivate() { }

    public virtual void Fire1() { }

    public virtual void Fire2() { }

    public virtual void Visuals1() { }

    public virtual void Visuals2() { }

    public virtual void StopFire1() { }

    public virtual void StopFire2() { }
}
