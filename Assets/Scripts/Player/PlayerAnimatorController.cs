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
        // force le binding avec l'avatar
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void Update()
    {
        Vector3 v = rb.linearVelocity; 
        v.y = 0f;
        animator.SetFloat(speedParam, v.magnitude);
    }

    public void TriggerAttack()
    {
        animator.ResetTrigger(attackTrigger);
        animator.SetTrigger(attackTrigger);
    }

    public void SetDead(bool dead)
    {
        animator.SetBool(deadBool, dead);
    }
}
