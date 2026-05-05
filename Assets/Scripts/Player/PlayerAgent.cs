using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

[RequireComponent(typeof(PlayerAgentRefs))]
public class PlayerAgent : Agent
{
    [Header("Config (single source of truth)")]
    public PlayerAgentConfig config;

    private PlayerAgentRefs refs;
    private EpisodeResetManager resetManager;

    public bool AttackIntentThisStep { get; private set; }
    private bool attackIntentLastStep;

    private Transform nearestTree;
    private Transform nearestEnemy;

    private readonly Dictionary<int, int> rewardedHitsPerEnemy = new Dictionary<int, int>();

    [Header("Slash Execution")]
    [Tooltip("Sword hitbox GameObject (trigger collider + SwordHitboxRelay). Keep DISABLED by default.")]
    [SerializeField] private GameObject swordHitbox;
    private float hitboxDisableAt;

    [Header("Animation (optional)")]
    [SerializeField] private PlayerAnimatorController animatorController;

    [Header("Harvest (optional)")]
    [SerializeField] private PlayerHarvest playerHarvest;

    [Header("Debug - Rays")]
    [SerializeField] private bool drawRayDebug = false;
    [SerializeField] private float rayDebugDuration = 0f;
    [SerializeField] private bool drawMissRays = true;

    // Movement lock during slash, valeur sentinelle, elle est set plus tard
    private float moveLockedUntil = -1f;
    private bool IsMoveLocked => Time.time < moveLockedUntil;

    // pour voir si il ne touche rien avec son swing, il a un reward negatif
    private bool swingHitSomething;


    public override void Initialize()
    {
        refs = GetComponent<PlayerAgentRefs>();
        refs.ValidateOrThrow();

        resetManager = FindFirstObjectByType<EpisodeResetManager>();
        if (resetManager == null)
            throw new MissingReferenceException("PlayerAgent requires an EpisodeResetManager in the scene.");

        if (config == null)
            config = resetManager.GetCurrentConfig();

        if (config == null)
            throw new MissingReferenceException("PlayerAgent.config is required (PlayerAgentConfig).");

        config.ValidateRuntime();
        MaxStep = config.maxStep;

        if (animatorController == null)
            animatorController = GetComponent<PlayerAnimatorController>();

        if (playerHarvest == null)
            playerHarvest = GetComponentInChildren<PlayerHarvest>(true);

        refs.maxHp = config.playerMaxHp;
        refs.hp = Mathf.Clamp(refs.hp, 0f, refs.maxHp);

        refs.woodTracker.SetTarget(config.targetWood);

        // ajout de la méthode, comme si on l'assigne.
        refs.woodTracker.OnWoodCollected += OnWoodCollected;
        if (config.endEpisodeOnTargetWood)
            refs.woodTracker.OnReachedTarget += OnReachedTarget;

        if (swordHitbox != null)
            swordHitbox.SetActive(false);

        moveLockedUntil = -1f;
    }

    private void OnDestroy()
    {
        if (refs != null && refs.woodTracker != null)
        {
            // comme dans initialize on les additionnes, dans le destroy on les enlèves
            refs.woodTracker.OnWoodCollected -= OnWoodCollected;
            refs.woodTracker.OnReachedTarget -= OnReachedTarget;
        }
    }

    public override void OnEpisodeBegin()
    {
        if (resetManager == null)
            throw new MissingReferenceException("EpisodeResetManager missing at OnEpisodeBegin.");

        resetManager.ResetEnvironment();

        rewardedHitsPerEnemy.Clear();
        AttackIntentThisStep = false;
        attackIntentLastStep = false;
        moveLockedUntil = -1f;
        swingHitSomething = false;

    }

    private void FixedUpdate()
    {
        if (refs == null || config == null) return;

        if (refs.hp > 0f)
        {
            refs.hp -= config.hpDrainPerSecond * Time.fixedDeltaTime;
            if (refs.hp <= 0f)
            {
                refs.hp = 0f; // assure que quand il recommence il n'est pas pénalisé dès le départ à cause du dernier run
                AddReward(config.penaltyDeath);
                EndEpisode();
            }
        }

        // quand il swing
        if (swordHitbox != null && swordHitbox.activeSelf && Time.time >= hitboxDisableAt)
        {
            if (!swingHitSomething)
                AddReward(config.penaltyMissSwing);
            swordHitbox.SetActive(false);
        }
            
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // normalisé,car c'est aide l'entrainement, les neural networks 
        sensor.AddObservation(Mathf.Clamp01(refs.hp / Mathf.Max(1f, refs.maxHp)));
        sensor.AddObservation(Mathf.Clamp01(refs.woodTracker.CollectedWood / (float)Mathf.Max(1, config.targetWood)));
        sensor.AddObservation(IsStandingOnWater());

        // le episode reset manager cache la position des arbres et ennemies pour rendre plus efficient la collect d'information
        nearestTree = resetManager.GetNearestTree(transform.position, config.maxRelevantDistance);
        nearestEnemy = resetManager.GetNearestEnemy(transform.position, config.maxRelevantDistance);

        AddTargetObs(sensor, nearestTree);
        AddTargetObs(sensor, nearestEnemy);

        AddRays(sensor);
    }

    private float IsStandingOnWater()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, config.groundCheckDistance))
        {
            // convertit le layer à son nombre, 0 si le ils n'ont pas le même nombre sinon, 1
            bool isWater = ((1 << hit.collider.gameObject.layer) & config.waterMask) != 0;
            return isWater ? 1f : 0f;
        }

        return 1f;
    }

    private void AddTargetObs(VectorSensor sensor, Transform t)
    {
        // s'il n'a rien trouver
        if (t == null)
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(1f);
            return;
        }

        Vector3 delta = t.position - transform.position;
        Vector3 localDir = transform.InverseTransformDirection(delta);
        Vector2 localXZ = new Vector2(localDir.x, localDir.z);
        float dist = localXZ.magnitude;

        sensor.AddObservation(1f);
        sensor.AddObservation(Mathf.Clamp(localXZ.x / config.maxRelevantDistance, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp(localXZ.y / config.maxRelevantDistance, -1f, 1f));
        sensor.AddObservation(Mathf.Clamp01(dist / config.maxRelevantDistance)); // normaliser
    }

    private void AddRays(VectorSensor sensor)
    {
        int n = Mathf.Clamp(config.rayCount, 0, 64);
        if (n == 0) return;

        float step = 360f / n;
        Vector3 origin = transform.position + Vector3.up * 0.25f;

        for (int i = 0; i < n; i++)
        {
            float ang = i * step;
            Vector3 dir = Quaternion.Euler(0f, ang, 0f) * transform.forward;

            // le out c'est si il hit quelque chose, ca collecte les données
            bool hit = Physics.Raycast(origin, dir, out RaycastHit rh, config.rayLength, config.rayMask);
            float normDist = hit ? Mathf.Clamp01(rh.distance / config.rayLength) : 1f;

            sensor.AddObservation(hit ? 1f : 0f);
            sensor.AddObservation(normDist);

            if (drawRayDebug)
            {
                float drawLen = hit ? rh.distance : config.rayLength;
                if (hit || drawMissRays)
                    Debug.DrawRay(origin, dir.normalized * drawLen, hit ? Color.green : Color.red, rayDebugDuration);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        var c = actions.ContinuousActions;

        float moveX = c.Length > 0 ? Mathf.Clamp(c[0], -1f, 1f) : 0f;
        float moveZ = c.Length > 1 ? Mathf.Clamp(c[1], -1f, 1f) : 0f;
        float atk = c.Length > 2 ? c[2] : 0f;

        AttackIntentThisStep = atk > 0.5f;

        if (AttackIntentThisStep) TryExecuteSlash();

        // si il est encore dans le cool down period
        Vector3 input;
        if (IsMoveLocked)
        {
            input = Vector3.zero;
        }
        else
        {
            input = Vector3.ClampMagnitude(new Vector3(moveX, 0f, moveZ), 1f);

            if (input.sqrMagnitude > 0.0005f)
            {
                // tourne
                Quaternion targetRot = Quaternion.LookRotation(input.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    config.rotateSpeedDegPerSec * Time.fixedDeltaTime
                );
            }
        }

        Vector3 planarVel = input * config.moveSpeed;
        Vector3 v = refs.rb.linearVelocity;
        refs.rb.linearVelocity = new Vector3(planarVel.x, v.y, planarVel.z);

        AddReward(config.stepPenalty);

        if (IsStandingOnWater() > 0.5f)
            AddReward(config.waterStepPenalty);
    }

    private void TryExecuteSlash()
    {
       

        if (refs == null || refs.slashCooldown == null) return;
        if (!refs.slashCooldown.TrySlash()) return;

        swingHitSomething = false;

        moveLockedUntil = Time.time + refs.slashCooldown.CooldownTime;

        if (refs.rb != null)
        {
            Vector3 v = refs.rb.linearVelocity;
            refs.rb.linearVelocity = new Vector3(0f, v.y, 0f);
        }

        animatorController?.TriggerAttack();

        if (playerHarvest != null)
        {
            bool hitTree = playerHarvest.TryHarvest(out _);
            swingHitSomething |= hitTree;
        }

        if (swordHitbox != null)
        {
            swordHitbox.SetActive(true);
            hitboxDisableAt = Time.time + config.playerHitboxActiveSeconds;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var c = actionsOut.ContinuousActions;
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        bool attack = Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0);

        c[0] = Mathf.Clamp(x, -1f, 1f);
        c[1] = Mathf.Clamp(z, -1f, 1f);
        c[2] = attack ? 1f : 0f;
    }

    private void OnWoodCollected(int oldValue, int newValue)
    {
        int gained = Mathf.Max(0, newValue - oldValue);
        if (gained > 0)
            AddReward(config.rewardPerWoodPickup * gained);
    }

    private void OnReachedTarget()
    {
        AddReward(config.rewardWin);
        EndEpisode();
    }

    public void NotifyTookDamage(float amount)
    {
        refs.hp = Mathf.Clamp(refs.hp - Mathf.Max(0f, amount), 0f, refs.maxHp);
        if (refs.hp <= 0f)
        {
            AddReward(config.penaltyDeath);
            EndEpisode();
        }
    }

    public void NotifyEnemyHit(GameObject enemy, float damageAmount)
    {
        if (enemy == null) return;
        
        // Mark the current swing as successful if we are within the active hitbox window.
        if (swordHitbox != null && swordHitbox.activeSelf)
            swingHitSomething = true;

        int id = enemy.GetInstanceID();
        rewardedHitsPerEnemy.TryGetValue(id, out int count);

        if (count >= config.maxRewardedHitsPerEnemy)
            return;

        rewardedHitsPerEnemy[id] = count + 1;
        AddReward(config.rewardEnemyHit);
    }

    public void NotifyEnemyKilled(GameObject enemy)
    {
        if (enemy == null) return;

        AddReward(config.rewardEnemyKill);

        float healAmount = refs.maxHp * Mathf.Clamp01(config.healOnEnemyKillFraction);
        refs.hp = Mathf.Min(refs.hp + healAmount, refs.maxHp);

        rewardedHitsPerEnemy.Remove(enemy.GetInstanceID());
    }
}