using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(EnemyMotorBehavior))]
[RequireComponent(typeof(EnemyPerceptionScan))]
public class EnemyBrainLogic : MonoBehaviour
{
    [SerializeField] private bool enemyIsEnabled = true;
  
    [SerializeField] private Transform player;

    [SerializeField] private GameObject enemySwordHitbox;

    [SerializeField] private EnemyAnimatorController animationController;

    // random autour du patrolIntervalTime pour désynchroniser les ennemis, car c'est moche visuellement que tout bouge en meme temps
    // ex: 0.25 => intervalle = patrolIntervalTime ± 0.25 secondes
    [SerializeField] private float patrolIntervalRandomRange = 0.25f;

    // Meilleure méthode pour classer les différents états
    private enum State { PatrolWait, PatrolMove, Chase, AttackWindup, Recover }

    private EnemyMotorBehavior motor;
    private EnemyPerceptionScan perception;

    // distance que l'épée se rend avec l'animation et
    //l'intervalle d'angle lorsqu'il "swing" l'épée
    private float attackRange = 1.5f;
    private float stopDistance = 1.0f;
    private float attackArcDegrees = 120f;

    // donnée pour les états
    private float memorySeconds = 1.5f;
    private float attackCooldown = 3.0f;
    private float windupSeconds = 0.15f;
    private float recoverSeconds = 1.5f;


    private float turningSpeed = 360f;


    // données pour l'état initiale: Patrol
    private float patrolIntervalTime = 2.0f;
    private float patrolMoveDistance = 3.0f;
    private bool alternatePatrolDirection = true;
    private float patrolArriveThreshold = 0.35f;

    // On ne l'active pas tout le temps, pour assurer qu'il ne donne pas des degats par accident 
    private float hitboxActiveTime = 1.0f;
    private string playerTag = "Player";

    private Rigidbody rb;
    private State state = State.PatrolWait;

    private float lastSeenTime = -999f;
    private Vector3 lastKnownPos;

    private float cooldownRemaining = 0f;
    private float stateTimer = 0f;

    private Vector3 patrolDestination;
    private int patrolDirSign = 1;

    // c'est le moment dans le temps qu'il faudrait disable le hitbox<
    // -1 au début, car c'est une valeur sentinentelle
    //c'est modifier lorsqu'il attaque:  hitboxDisableAt = Time.time + hitboxActiveTime;
    private float hitboxDisableAt = -1f;

    public void ApplyConfig(PlayerAgentConfig cfg)
    {
        if (cfg == null) return;

        attackRange = cfg.enemyAttackRange;
        stopDistance = cfg.enemyStopDistance;
        attackArcDegrees = cfg.enemyAttackArcDegrees;

        memorySeconds = cfg.enemyMemorySeconds;
        attackCooldown = cfg.enemyAttackCooldown;
        windupSeconds = cfg.enemyWindupSeconds;
        recoverSeconds = cfg.enemyRecoverSeconds;

        patrolIntervalTime = cfg.enemyPatrolIntervalSeconds;
        patrolMoveDistance = cfg.enemyPatrolMoveDistance;
        alternatePatrolDirection = cfg.enemyAlternatePatrolDirection;
        patrolArriveThreshold = cfg.enemyPatrolArriveThreshold;

        hitboxActiveTime = cfg.enemyHitboxActiveSeconds;
        playerTag = cfg.playerTag;

        // tres couteux, mais ne devrais pas arriver, c'est vraiment dernier recours
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag(playerTag);
            if (p != null) player = p.transform;
        }
    }


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        motor = GetComponent<EnemyMotorBehavior>();
        perception = GetComponent<EnemyPerceptionScan>();

        //je ne sais pas pourquoi il faut ajouter le true, mais seul facon que ca fonctionne
        if (animationController == null) animationController = GetComponentInChildren<EnemyAnimatorController>(true);

        Debug.Assert(enemySwordHitbox != null);
        stateTimer = GetRandomPatrolIntervalTime();

        // assure que le hitbox est désactivé au début
        ForceDisableHitbox();

        if (!enemyIsEnabled)
            motor.SetDesiredDirection(Vector3.zero);
    }

    public void ForceDisableHitbox()
    {
        // toujours vérifier si ce n'est pas null, car c'est public
        if (enemySwordHitbox != null)
            enemySwordHitbox.SetActive(false);

        hitboxDisableAt = -1f;
    }

    private void Update()
    {
        if (!enemyIsEnabled) return;

        UpdateTimers();

        if (player != null)
        {
            bool seenNow = perception.UpdatePerception(player);

            if (seenNow || perception.playerDetected)
            {
                lastSeenTime = Time.time;
                lastKnownPos = player.position;
                lastKnownPos.y = transform.position.y;
            }
        }

        bool targetValid = (Time.time - lastSeenTime) <= memorySeconds;

        // pour assurer qu'il fait une chose a la fois
        if (targetValid && (state == State.PatrolWait || state == State.PatrolMove))
            state = State.Chase;

        switch (state)
        {
            case State.PatrolWait: HandlePatrolWait(targetValid); break;
            case State.PatrolMove: HandlePatrolMove(targetValid); break;
            case State.Chase: HandleChase(targetValid); break;
            case State.AttackWindup: HandleAttackWindup(targetValid); break;
            case State.Recover: HandleRecover(targetValid); break;
        }
    }

    private void UpdateTimers()
    {
        if (cooldownRemaining > 0f) cooldownRemaining -= Time.deltaTime;
        if (stateTimer > 0f) stateTimer -= Time.deltaTime;
    }

    //state 1
    private void HandlePatrolWait(bool targetValid)
    {
        motor.SetDesiredDirection(Vector3.zero);

        //si détecte enemi
        if (targetValid)
        {
            state = State.Chase;
            return;
        }
        // si pas d'enemi, attend que le timer pour ce state termine pour passer au prochain
        if (stateTimer > 0f) return;

        // pas d'enemi, et le temps du state s'est écouler, alors il va cherhcer un autre une autre positon pour patrol
        patrolDestination = ComputeNextPatrolDestination();
        state = State.PatrolMove;
    }

    //state 2
    private void HandlePatrolMove(bool validTarget)
    {
        if (validTarget)
        {
            state = State.Chase;
            return;
        }

        Vector3 parcours = patrolDestination - transform.position;

        //parcours.y = 0f;

        float dist = parcours.magnitude;

        //lorsqu'il arrive a sa destination
        if (dist <= patrolArriveThreshold)
        {
            motor.SetDesiredDirection(Vector3.zero);
            state = State.PatrolWait;
            stateTimer = GetRandomPatrolIntervalTime();
            return;
        }

        motor.SetDesiredDirection(parcours.normalized);
    }

    //state 3
    private void HandleChase(bool targetValid)
    {
        // si le target n'est plus valide, il attend (patrol)
        if (!targetValid)
        {
            state = State.PatrolWait;
            stateTimer = GetRandomPatrolIntervalTime();
            motor.SetDesiredDirection(Vector3.zero);
            return;
        }

        Vector3 toTarget = lastKnownPos - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        
        // important de mettre des seuils pour le déplacement, sinon peut bugg
        // on utlise pas stopDistance, car on veut ici qu'il fait face au joueur tout le temps
        // avec stopDistance il peut etre dans le range mais pas face a lui, alors il attaquerais dans le vide si 
        //l'angle n'est pas idéal
        Vector3 direction = (distance > 0.0001f) ? (toTarget / distance) : Vector3.zero;

        if (distance > stopDistance)
        {
            motor.SetDesiredDirection(direction);
            return;
        }

        motor.SetDesiredDirection(Vector3.zero);

        if (CanAttack(direction, distance))
            BeginWindup();
    }

    //state 4
    private void HandleAttackWindup(bool targetValid)
    {
        motor.SetDesiredDirection(Vector3.zero);
        if (targetValid) FaceTowards(lastKnownPos);

        if (stateTimer > 0f) return;

        OpenEnemyHitboxWindow();

        cooldownRemaining = attackCooldown;
        state = State.Recover;
        stateTimer = Mathf.Max(0f, recoverSeconds);
    }

    private void HandleRecover(bool targetValid)
    {
        motor.SetDesiredDirection(Vector3.zero);
        if (targetValid) FaceTowards(lastKnownPos);

        if (stateTimer > 0f) return;

        state = targetValid ? State.Chase : State.PatrolWait;
        if (state == State.PatrolWait)
            stateTimer = GetRandomPatrolIntervalTime();
    }
    private Vector3 ComputeNextPatrolDestination()
    {
        Vector3 basePosistion = transform.position;

        Vector3 direction;

        // méthode 1 possible fais des allé-retour
        if (alternatePatrolDirection)
        {
            direction = transform.forward * patrolDirSign;
            patrolDirSign *= -1;
        }
        //méthode 2, bouge dansun sens random
        else
        {
            float angle = Random.Range(0f, 360f);
            direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
        }

        //direction.y = 0f;
        direction.Normalize();

        Vector3 destination = basePosistion + direction * patrolMoveDistance;
        destination.y = basePosistion.y;
        return destination;
    }
    private bool CanAttack(Vector3 directionToTarget, float distanceToTarget)
    {
        if (cooldownRemaining > 0f) return false;
        if (distanceToTarget > attackRange) return false;

        if (directionToTarget.sqrMagnitude < 0.0001f) return true;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return true;

        forward.Normalize();
        float angle = Vector3.Angle(forward, directionToTarget.normalized);

        // car le arc est le total, on veu la moitier 
        return angle <= (attackArcDegrees * 0.5f);
    }

    private void BeginWindup()
    {
        state = State.AttackWindup;
        stateTimer = Mathf.Max(0f, windupSeconds);
        animationController?.TriggerAttack();
    }

    private void FaceTowards(Vector3 worldPosition)
    {
        Vector3 toTarget = worldPosition - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        float maxDeg = turningSpeed * Time.deltaTime;
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, maxDeg));
    }

    private void OpenEnemyHitboxWindow()
    {
        if (enemySwordHitbox == null) return;
        enemySwordHitbox.SetActive(true);
        hitboxDisableAt = Time.time + hitboxActiveTime;
    }

    private float GetRandomPatrolIntervalTime()
    {
        float t = patrolIntervalTime + Random.Range(-patrolIntervalRandomRange, patrolIntervalRandomRange);
        return Mathf.Max(0f, t);
    }

    private void FixedUpdate()
    {
        if (enemySwordHitbox != null && enemySwordHitbox.activeSelf && Time.time >= hitboxDisableAt)
            enemySwordHitbox.SetActive(false);
    }
}
