using UnityEngine;

public class PlayerCombatExecutor : MonoBehaviour
{
    [SerializeField] private PlayerAgent agent;
    [SerializeField] private PlayerAnimatorController animCtrl;

    private bool attackIntentLastFrame;

    private void Awake()
    {
        if (agent == null) agent = GetComponent<PlayerAgent>();
        if (animCtrl == null) animCtrl = GetComponent<PlayerAnimatorController>();
    }

    private void Update()
    {
        if (agent == null || animCtrl == null) return;

        // Rising-edge detection ONLY for animation
        bool attackIntent = agent.AttackIntentThisStep;
        bool attackStarted = attackIntent && !attackIntentLastFrame;

        if (attackStarted)
        {
            animCtrl.TriggerAttack();
        }

        attackIntentLastFrame = attackIntent;
    }


}
