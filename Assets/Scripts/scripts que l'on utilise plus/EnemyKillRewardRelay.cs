using UnityEngine;

public class EnemyKillRewardRelay : MonoBehaviour
{
    [SerializeField] private PlayerAgent agent;
    
    // je ne vérifie pas dans Awake si agent n'est pas null, car quand ca reset chauqe épisode ca levera des exceptions

    public void NotifyKilled()
    {
        if (agent != null)
            agent.NotifyEnemyKilled(gameObject);
    }
}
