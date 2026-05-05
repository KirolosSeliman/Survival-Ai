using UnityEngine;

public class PlayerDamageRelay : MonoBehaviour
{
    [SerializeField] private PlayerAgent agent;

    [Header("Incoming Damage")]
    public float damagePerHit = 10f;

    [Header("What counts as enemy damage")]
    public string enemyWeaponTag = "Enemy Weapon"; // set if you have one
    public string enemyTag = "Enemy";             // fallback

    
    private void Awake()
    {
        if (agent == null) agent = GetComponentInParent<PlayerAgent>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (agent == null || other == null) return;

        
        if (other.CompareTag(enemyWeaponTag) || other.CompareTag(enemyTag))
        {
            agent.NotifyTookDamage(damagePerHit);
        }
    }
}
