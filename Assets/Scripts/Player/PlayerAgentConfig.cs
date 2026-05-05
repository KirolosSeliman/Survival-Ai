using UnityEngine;

[CreateAssetMenu(menuName = "ML/Player Agent Config", fileName = "PlayerAgentConfig")]
public class PlayerAgentConfig : ScriptableObject
{
    // =========================
    // EPISODE
    // =========================
    [Header("Episode")]
    [Min(1)] public int maxStep = 15000;
    [Min(0f)] public float hpDrainPerSecond = 0.2f;
    public float stepPenalty = -0.00002f;
    public bool endEpisodeOnTargetWood = true;

    // =========================
    // PLAYER
    // =========================
    [Header("Player - Movement")]
    [Min(0.01f)] public float moveSpeed = 5.0f;
    [Min(0.01f)] public float rotateSpeedDegPerSec = 720f;

    [Header("Player - Health")]
    [Min(1f)] public float playerMaxHp = 100f;

    [Header("Player - Slash / Combat")]
    [Min(0.01f)] public float slashCooldownSeconds = 1f;
    [Min(0.01f)] public float playerHitboxActiveSeconds = 1f;
    [Min(0f)] public float playerDamageToEnemy = 10f;
    [Range(0f, 1f)] public float healOnEnemyKillFraction = 0.04f;

    // =========================
    // HARVEST / TREES / WOOD
    // =========================
    [Header("Harvesting")]
    [Min(0.1f)] public float harvestMaxDistance = 2.25f;
    public bool harvestPreferClosestTree = true;
    [Range(8, 256)] public int harvestMaxHits = 64;

    [Header("Trees / Wood")]
    [Min(1)] public int targetWood = 10;
    [Min(1)] public int treeMaxWood = 3;
    [Min(1)] public int treeWoodPerHit = 1;
    [Min(0f)] public float treeDropRadius = 1.75f;
    [Min(0f)] public float treeDropYOffset = 0.25f;

    // =========================
    // REWARDS / SHAPING
    // =========================
    [Header("Rewards")]
    public float rewardWin = 1.0f;
    public float penaltyDeath = -1.0f;
    public float rewardPerWoodPickup = 0.05f;
    public float rewardEnemyHit = 0.003f;
    public float rewardEnemyKill = 0.10f;
    public float waterStepPenalty = -0.00005f;

    [Tooltip("Negative reward applied when the agent performs a slash that hits neither an enemy nor a tree.")]
    public float penaltyMissSwing = -0.002f;

    [Header("Anti-Farming")]
    [Min(0)] public int maxRewardedHitsPerEnemy = 10;

    // =========================
    // OBSERVATIONS
    // =========================
    [Header("Observations")]
    [Min(1f)] public float maxRelevantDistance = 25f;

    [Header("Ray Perception")]
    [Range(0, 64)] public int rayCount = 10;
    [Min(0.1f)] public float rayLength = 20f;
    public LayerMask rayMask;

    // =========================
    // PHYSICS / LAYERS / TAGS
    // =========================
    [Header("Ground / Water")]
    public LayerMask waterMask;
    [Min(0.01f)] public float groundCheckDistance = 2.0f;

    [Header("Tree Query")]
    public LayerMask treeLayerMask;
    public bool requireTreeTag = true;
    public string treeTag = "Tree";

    [Header("Enemy Query")]
    public LayerMask enemyLayerMask;
    public string enemyTag = "Enemy";
    public string playerTag = "Player";

    // =========================
    // ENEMY DIFFICULTY
    // =========================
    [Header("Enemy - Health / Damage")]
    [Min(1f)] public float enemyMaxHealth = 30f;
    [Min(0f)] public float enemyDamageToPlayer = 5f;

    [Header("Enemy - Movement")]
    [Min(0.01f)] public float enemyMoveSpeed = 2f;
    [Min(0.01f)] public float enemyTurnSpeedDegPerSec = 90f;

    [Header("Enemy - Brain")]
    [Min(0.1f)] public float enemyAttackRange = 1.5f;
    [Min(0.1f)] public float enemyStopDistance = 1.0f;
    [Range(10f, 180f)] public float enemyAttackArcDegrees = 120f;
    [Min(0f)] public float enemyMemorySeconds = 1.5f;
    [Min(0f)] public float enemyAttackCooldown = 3.0f;
    [Min(0f)] public float enemyWindupSeconds = 0.15f;
    [Min(0f)] public float enemyRecoverSeconds = 0.20f;
    [Min(0.01f)] public float enemyHitboxActiveSeconds = 1.0f;

    [Header("Enemy - Patrol")]
    [Min(0f)] public float enemyPatrolIntervalSeconds = 2.0f;
    [Min(0f)] public float enemyPatrolMoveDistance = 3.0f;
    public bool enemyAlternatePatrolDirection = true;
    [Min(0.01f)] public float enemyPatrolArriveThreshold = 0.35f;

    [Header("Enemy - Perception")]
    [Min(0.1f)] public float enemyVisionRange = 15f;
    [Range(1f, 360f)] public float enemyRadarRangeAngle = 90f;
    [Min(0.1f)] public float enemyRadarTurningSpeed = 90f;

    // =========================
    // SPAWNING / RESET
    // =========================
    [Header("Enemy Spawning Near Trees")]
    [Range(0f, 1f)] public float enemySpawnChancePerTree = 1.0f;
    [Min(0f)] public float enemySpawnOffsetRadius = 2.0f;

    [Header("Curriculum Thresholds (Episodes)")]
    [Min(0)] public int easyUntilEpisode = 2000;
    [Min(0)] public int midUntilEpisode = 6000;

    public void ValidateRuntime()
    {
        maxStep = Mathf.Max(1, maxStep);

        playerMaxHp = Mathf.Max(1f, playerMaxHp);
        enemyMaxHealth = Mathf.Max(1f, enemyMaxHealth);

        moveSpeed = Mathf.Max(0.01f, moveSpeed);
        rotateSpeedDegPerSec = Mathf.Max(0.01f, rotateSpeedDegPerSec);

        slashCooldownSeconds = Mathf.Max(0.01f, slashCooldownSeconds);
        playerHitboxActiveSeconds = Mathf.Max(0.01f, playerHitboxActiveSeconds);

        harvestMaxDistance = Mathf.Max(0.1f, harvestMaxDistance);
        harvestMaxHits = Mathf.Clamp(harvestMaxHits, 8, 256);

        targetWood = Mathf.Max(1, targetWood);
        treeMaxWood = Mathf.Max(1, treeMaxWood);
        treeWoodPerHit = Mathf.Max(1, treeWoodPerHit);

        enemyMoveSpeed = Mathf.Max(0.01f, enemyMoveSpeed);
        enemyTurnSpeedDegPerSec = Mathf.Max(0.01f, enemyTurnSpeedDegPerSec);

        enemyAttackRange = Mathf.Max(0.1f, enemyAttackRange);
        enemyStopDistance = Mathf.Max(0.1f, enemyStopDistance);

        enemyVisionRange = Mathf.Max(0.1f, enemyVisionRange);
        enemyRadarRangeAngle = Mathf.Clamp(enemyRadarRangeAngle, 1f, 360f);
        enemyRadarTurningSpeed = Mathf.Max(0.1f, enemyRadarTurningSpeed);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ValidateRuntime();
    }
#endif
}
