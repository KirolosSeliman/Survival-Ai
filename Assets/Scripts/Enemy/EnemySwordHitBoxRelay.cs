using System.Collections.Generic;
using UnityEngine;

public class EnemySwordHitBoxRelay : MonoBehaviour
{
    [SerializeField] private float playerDamage = 5f;

    [SerializeField] private string playerTag = "Player";

    private readonly HashSet<int> hitThisSwing = new HashSet<int>();

    public void ApplyConfig(PlayerAgentConfig cfg)
    {
        if (cfg == null) return;
        playerDamage = cfg.enemyDamageToPlayer;
        playerTag = cfg.playerTag;
    }

    // clear le hashSet a chaque fois que le hit window commence
    //car le game object ou cette composante vie s'active seulement lorsque l'enemi attaque pour éviter que 
    //son épée fait des dégat inintentionnellement
    private void OnEnable()
    {
        hitThisSwing.Clear();
    }
    private void OnDisable() => hitThisSwing.Clear();

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || other.isTrigger) return;
        var playerAgent = other.GetComponentInParent<PlayerAgent>();
        if (playerAgent == null) return;

        //belle facon de noter que c'est l'agent qui l'a touché
        int id = playerAgent.gameObject.GetInstanceID();
        if (!hitThisSwing.Add(id)) return;


        playerAgent.NotifyTookDamage(playerDamage);
    }
}
