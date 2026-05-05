using UnityEngine;

public class PlayerAnimatorController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody rb;

    [Header("Animator Params")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string deadBool = "IsDead"; 

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (rb == null) rb = GetComponentInParent<Rigidbody>();
    }

    private void Start()
    {
        // Forces Animator to bind to the Avatar/rig on play.
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void Update()
    {
        if (animator == null || rb == null) return;

        Vector3 v = rb.linearVelocity; // more standard than linearVelocity
        v.y = 0f;
        animator.SetFloat(speedParam, v.magnitude);
    }

    public void TriggerAttack()
    {
        if (animator == null) return;
        animator.ResetTrigger(attackTrigger);
        animator.SetTrigger(attackTrigger);
    }

    public void SetDead(bool dead)
    {
        if (animator == null) return;
        animator.SetBool(deadBool, dead);
    }
}
