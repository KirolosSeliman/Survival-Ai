using System;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 30f;
    [SerializeField] private bool destroyOnDeath = true;
    [SerializeField] private float destroyDelay = 0.1f;

    [SerializeField] private PlayerAgent playerAgent;

    public float Current { get; private set; }
    public float Max => maxHealth;

    public event Action<float, float> OnHealthChanged;
    public event Action OnDied;

    private bool notifiedKill;

    private void Awake()
    {
        Current = maxHealth;
        OnHealthChanged?.Invoke(Current, maxHealth);
    }

    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        Current = Mathf.Min(Current, maxHealth);
        OnHealthChanged?.Invoke(Current, maxHealth);
    }

    public void SetPlayerAgent(PlayerAgent agent) => playerAgent = agent;
    
    public void ResetToFull()
    {
        notifiedKill = false;
        Current = maxHealth;
        OnHealthChanged?.Invoke(Current, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (Current <= 0f) return;

        // la maniere la plus sur de s'assurer qu'il n'y ai pas d ebug et qu'il perd plus de vie que voulu
        Current = Mathf.Clamp(Current - Mathf.Max(0f, amount), 0f, maxHealth);
        OnHealthChanged?.Invoke(Current, maxHealth);

        if (Current <= 0f)
        {
            OnDied?.Invoke();

            if (!notifiedKill && playerAgent != null)
            {
                notifiedKill = true;
                playerAgent.NotifyEnemyKilled(gameObject);
            }

            if (destroyOnDeath)
                Destroy(gameObject, destroyDelay);
        }
    }
}
