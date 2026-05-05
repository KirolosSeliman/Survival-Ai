using UnityEngine;
/// <summary>
/// Cette composante bouge simplement le Rigid body assigné
/// Elle ne décide pas la direction, EnmeyBrainLogic lui donne le vecteur
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyMotorBehavior : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float turnAngleSpeed = 90f;

    private Rigidbody rb;
    private Vector3 brainDesiredMove = Vector3.zero;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // rends le mouvement plus fluid
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        
        // utile lorsque le Rigid body bouge beaucoup
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    //Pour le Manager
    public void ApplyConfig(PlayerAgentConfig cfg)
    {
        if (cfg == null) return;
        moveSpeed = cfg.enemyMoveSpeed;
        turnAngleSpeed = cfg.enemyTurnSpeedDegPerSec;
    }

    /// <summary>
    /// C'est la fonction que le EnemyBrainLogic utilise pour parler avec cette composante
    /// </summary>
    /// <param name="directionInput"> Le vecteur de déplacement donné</param>
    public void SetDesiredDirection(Vector3 directionInput)
    {
        directionInput.y = 0f;
        brainDesiredMove = directionInput.sqrMagnitude > 0f ? directionInput.normalized : Vector3.zero;
    }

    /// <summary>
    /// Déplace vers le point désirer et rotationne pour qu'il fait toujours face devant lui
    /// </summary>
    private void FixedUpdate()
    {
        // early return
        if (brainDesiredMove == Vector3.zero)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        
        rb.linearVelocity = new Vector3(brainDesiredMove.x * moveSpeed, rb.linearVelocity.y, brainDesiredMove.z * moveSpeed);

        // le brainDesiredMove est le forward vecteur et le vecteur "up" sera toujours orthogonale rigid Body
        Quaternion targetRotation = Quaternion.LookRotation(brainDesiredMove, Vector3.up);
        float turningSpeed = turnAngleSpeed * Time.deltaTime;
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, turningSpeed));
    }
}
