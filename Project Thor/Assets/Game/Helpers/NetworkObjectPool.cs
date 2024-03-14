using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkObjectPool : NetworkBehaviour
{
    public List<GameObject> pooledObjects;
    public List<IBaseNetworkObject> pooledNetworkObjects;
    public GameObject objectToPool;
    public int amountToPool;

    // Start is called before the first frame update
    void Start()
    {
        if(!IsServer)
        {
            return;
        }

        pooledObjects = new List<GameObject>();
        pooledNetworkObjects = new List<IBaseNetworkObject>();
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
