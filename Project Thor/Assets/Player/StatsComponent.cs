using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class StatsComponent : NetworkBehaviour
{
    public int MaxHealth;
    public NetworkVariable<int> Health = new NetworkVariable<int>();

    // Start is called before the first frame update
    void Start()
    {
        if(IsServer)
        {
            Health.Value = MaxHealth;
        }
    }

    public override void OnNetworkSpawn()
    {
        Health.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        Health.OnValueChanged -= OnHealthChanged;
    }

    public void OnHealthChanged(int previous, int current)
    {
        print(current);
    }
}
