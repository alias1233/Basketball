using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseWeapon : MonoBehaviour
{
    [SerializeField]
    public WeaponManager Manager;

    [SerializeField]
    public GameObject WeaponModel;
    [SerializeField]
    public PlayerMovement PlayerMovementComponent;

    public List<PlayerManager> RewindedPlayerList;
    public LayerMask PlayerLayer;

    public Vector3 Offset;

    public int Range1;
    public int Range2;

    public int AmmoCount;

    public int FireCooldown1;
    public int FireCooldown2;

    public int LastTimeShot1;
    public int LastTimeShot2;

    public float Damage;
    public float Damage2;

    private RaycastHit[] Hits = new RaycastHit[5];

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

        int RewindedPlayers = 0;

        for (int i = 0; i < NumHits; i++)
        {
            if (Hits[i].transform.gameObject.TryGetComponent<PlayerManager>(out PlayerManager rewind))
            {
                if (rewind.RewindToPosition(Manager.GetTeam(), Manager.GetPingInTick()))
                {
                    RewindedPlayerList.Add(rewind);

                    RewindedPlayers++;
                }
            }
        }

        if(RewindedPlayers > 0)
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

    public virtual void OnActivate() { }

    public virtual void OnDeactivate() { }

    public virtual void Fire1() { }

    public virtual void Fire2() { }

    public virtual void StopFire1() { }

    public virtual void StopFire2() { }
}
