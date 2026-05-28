using UnityEngine;

public class PlayerAgentRefs : MonoBehaviour
{
    /*Tous ce qui est important pour le joueur est regroupé ici
     ça permet d'accéder les choses importantes du joueur comme sa vie.
    Par exemple dans PlayerAgent, on bouge le joueur en accédant à son RigidBody
    ici. */
    public Rigidbody rb;
    public WoodTracker woodTracker;
    public SlashCoolDown slashCooldown;

    
    public Animator animator;
    public Collider swordHitbox;
    public Transform body;
    public PlayerHarvest harvest;

    public float maxHp = 100f;
    public float hp = 100f;

    public void ValidateOrThrow()
    {
        if (rb == null) 
            throw new MissingReferenceException("Besoin d'un RigidBody");
        if (woodTracker == null) 
            throw new MissingReferenceException("Besoin de PlayerAgentRefs.woodTracker");
        if (slashCooldown == null) 
            throw new MissingReferenceException("Besoin de PlayerAgentRefs.slashCooldown");
        if (body == null) 
            body = transform;
        if (harvest == null)
            harvest = GetComponentInChildren<PlayerHarvest>(true);
        if (maxHp <= 0f) 
            throw new System.ArgumentOutOfRangeException(nameof(maxHp), "le HP max devrait être plus que 0");

        hp = Mathf.Clamp(hp, 0f, maxHp);
    }
}
