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
        if (agent.config == null) return;
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
        if (other == null) return;

        int id = other.GetInstanceID();

        if (hitThisWindow.Contains(id)) return;
        hitThisWindow.Add(id);

        if (!other.CompareTag(enemyTag))
            return;

        var hp = other.GetComponentInParent<EnemyHealth>();

        agent.NotifyEnemyHit(hp.gameObject, enemyDamage);
        hp.TakeDamage(enemyDamage);
    }
}
