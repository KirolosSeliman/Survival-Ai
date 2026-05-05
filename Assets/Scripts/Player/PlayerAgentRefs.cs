using UnityEngine;

public class PlayerAgentRefs : MonoBehaviour
{
    [Header("Required")]
    public Rigidbody rb;
    public WoodTracker woodTracker;
    public SlashCoolDown slashCooldown;

    [Header("Optional / Recommended")]
    public Animator animator;
    public Collider swordHitbox;
    public Transform body;
    public PlayerHarvest harvest;

    [Header("Health")]
    public float maxHp = 100f;
    public float hp = 100f;

    [Header("Boat Progress")]
    public Transform boatTransform;

    public void ValidateOrThrow()
    {
        if (rb == null) throw new MissingReferenceException("PlayerAgentRefs.rb is required.");
        if (woodTracker == null) throw new MissingReferenceException("PlayerAgentRefs.woodTracker is required.");
        if (slashCooldown == null) throw new MissingReferenceException("PlayerAgentRefs.slashCooldown is required.");

        if (body == null) body = transform;

        if (harvest == null)
            harvest = GetComponentInChildren<PlayerHarvest>(true);

        if (maxHp <= 0f) throw new System.ArgumentOutOfRangeException(nameof(maxHp), "maxHp must be > 0.");

        hp = Mathf.Clamp(hp, 0f, maxHp);
    }
}
