using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class StatsComponent : NetworkBehaviour
{
    public int MaxHealth;
    public NetworkVariable<int> Health = new NetworkVariable<int>();

    [SerializeField]
    private TMP_Text HealthBarText;

    // Start is called before the first frame update
    void Start()
    {
        if(IsServer)
        {
            Health.Value = MaxHealth;
        }

        HealthBarText.text = MaxHealth.ToString();
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
        HealthBarText.text = current.ToString() + " / " + MaxHealth.ToString();
    }

    public void Damage(int damage)
    {
        if(!IsServer)
        {
            return;
        }

        Health.Value -= damage;

        if(Health.Value < 0)
        {
            Die();

            Health.Value = 0;
        }
    }

    private void Die()
    {

    }
}
