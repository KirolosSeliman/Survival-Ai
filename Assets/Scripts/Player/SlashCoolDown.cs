using UnityEngine;
/// <summary>
/// Cette classe gere le cooldown du player slash de maniere séparé
/// Elle facilite la clareté du code
/// </summary>
public class SlashCoolDown : MonoBehaviour
{
    [SerializeField] private float slashCooldown = .5f;

    private float nextTimeReady;

    public bool IsReady => Time.time > nextTimeReady;
    public float CooldownTime => slashCooldown;

    private void Awake()
    {
        Debug.Assert(slashCooldown > 0);
    }

    public void ResetSlashCooldown()
    {
        nextTimeReady = 0f;
    }

    public void SetCooldown(float seconds)
    {
        slashCooldown = Mathf.Max(0.01f, seconds);
    }

    public bool TrySlash()
    {
        if (!IsReady) return false;
        nextTimeReady = Time.time + CooldownTime;
        return true;
    }
}
