using System.Collections.Generic;
using UnityEngine;

public class SwordHitboxRelay : MonoBehaviour
{
    [SerializeField] private PlayerAgent agent;

    private float enemyDamage;
    private string enemyTag;

    private readonly HashSet<int> hitThisWindow = new HashSet<int>();

    private void Awake()
    {
        if (agent == null) agent = GetComponentInParent<PlayerAgent>();
        ApplyFromAgentConfig();
    }

    private void ApplyFromAgentConfig()
    {
        if (agent == null || agent.config == null) return;
        enemyDamage = agent.config.playerDamageToEnemy;
        enemyTag = agent.config.enemyTag;
    }

    private void OnEnable()
    {
        hitThisWindow.Clear();
        ApplyFromAgentConfig();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (agent == null || other == null) return;

        int id = other.GetInstanceID();
        if (hitThisWindow.Contains(id)) return;
        hitThisWindow.Add(id);

        // Enemy-only. Trees are harvested by PlayerHarvest to avoid double drops/rewards.
        if (!other.CompareTag(enemyTag))
            return;

        var hp = other.GetComponentInParent<EnemyHealth>();
        if (hp == null || hp.Current <= 0f)
            return;

        agent.NotifyEnemyHit(hp.gameObject, enemyDamage);
        hp.TakeDamage(enemyDamage);
    }
}
