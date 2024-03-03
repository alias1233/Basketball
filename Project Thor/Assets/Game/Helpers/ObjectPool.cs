using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ObjectPool : NetworkBehaviour
{
    public List<GameObject> pooledObjects;
    public GameObject objectToPool;
    public bool bIsNetworkObject;
    public int amountToPool;

    void Start()
    {
        if(!IsServer && bIsNetworkObject)
        {
            return;
        }

        pooledObjects = new List<GameObject>();
        GameObject tmp;

        for (int i = 0; i < amountToPool; i++)
        {
            tmp = Instantiate(objectToPool);

            if(tmp.TryGetComponent<NetworkObject>(out NetworkObject obj))
            {
                obj.Spawn();

            }

            tmp.SetActive(false);
            pooledObjects.Add(tmp);
        }
    }

    public GameObject GetPooledObject()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            if (!pooledObjects[i].activeInHierarchy)
            {
                return pooledObjects[i];
            }
        }

        return null;
    }
}
