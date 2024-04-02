using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class NetworkObjectPool : NetworkBehaviour
{
    public List<GameObject> pooledObjects = new List<GameObject>();
    public List<IBaseNetworkObject> pooledNetworkObjects = new List<IBaseNetworkObject>();
    public GameObject objectToPool;
    public int amountToPool;

    public override void OnNetworkSpawn()
    {
        if(!IsServer)
        {
            return;
        }

        GameObject tmp;

        for (int i = 0; i < amountToPool; i++)
        {
            tmp = Instantiate(objectToPool);
            tmp.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
            NetworkBehaviour[] networkbehaviours = tmp.GetComponents<NetworkBehaviour>();
            IBaseNetworkObject Object = null;

            foreach (NetworkBehaviour j in networkbehaviours)
            {
                if(j is IBaseNetworkObject)
                {
                    Object = j as IBaseNetworkObject;

                    break;
                }
            }

            if(Object != null)
            {
                Object.Deactivate();
                pooledNetworkObjects.Add(Object);
                pooledObjects.Add(tmp);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
        {
            return;
        }

        for (int i = 0; i < amountToPool; i++)
        {
            //pooledObjects[i].GetComponent<NetworkObject>().Despawn(true);
        }
    }

    public GameObject GetPooledObject()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            if (!pooledNetworkObjects[i].bIsActive)
            {
                return pooledObjects[i];
            }
        }

        return null;
    }
}
