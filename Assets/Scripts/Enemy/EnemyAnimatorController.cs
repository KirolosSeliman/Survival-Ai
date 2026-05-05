using UnityEngine;

public class EnemyAnimatorController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;

    // Pour facilité
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string deadBool = "Dead";

    [SerializeField] private float maxMoveSpeed = 2f;

    public System.Action OnHitMoment;
    private float hitTimer = -1f;

    public void ApplyConfig(PlayerAgentConfig cfg)
    {
        if (cfg == null) return;
        maxMoveSpeed = Mathf.Max(0.01f, cfg.enemyMoveSpeed);
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

    }

    private void Update()
    {
        UpdateSpeed();
        TickHitMoment();
    }

    private void UpdateSpeed()
    {
        Vector3 v = rb.linearVelocity;
        v.y = 0f;

        float speed = v.magnitude;
        animator.SetFloat(speedParam, speed);
    }

    private void TickHitMoment()
    {
        if (hitTimer < 0f) return;

        hitTimer -= Time.deltaTime;
        if (hitTimer <= 0f)
        {
            hitTimer = -1f;
            OnHitMoment?.Invoke();
        }
    }
    public void TriggerAttack()
    {
        animator.ResetTrigger(attackTrigger);
        animator.SetTrigger(attackTrigger);
    }

    public void SetDead(bool dead)=> animator.SetBool(deadBool, dead);
    public void AnimationEvent_HitMoment() => OnHitMoment?.Invoke();
    
}
