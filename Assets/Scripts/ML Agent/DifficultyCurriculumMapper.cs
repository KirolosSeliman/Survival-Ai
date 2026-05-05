using UnityEngine;
using Unity.MLAgents;

[DisallowMultipleComponent]
public class DifficultyCurriculumMapper : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EpisodeResetManager resetManager;
    [SerializeField] private PlayerAgent player;

    [Header("Config")]
    [SerializeField] private PlayerAgentConfig baseConfigAsset;
    [SerializeField] private PlayerAgentConfig runtimeConfig;

    [Header("Environment Parameter")]
    [SerializeField] private string difficultyParamName = "difficulty";
    [SerializeField] private float difficultyMin = 0f;
    [SerializeField] private float difficultyMax = 10f;

    [Header("Override difficulty to Debug")]
    [SerializeField] private bool useInspectorDifficulty = false;
    [SerializeField] private float inspectorDifficulty = 6f;

    [Header("EASY (difficulty=0) overrides")]
    [SerializeField] private int easyTargetWood = 3;

    [SerializeField] private float easyEnemySpawnChancePerTree = 0.0f;
    [SerializeField] private float easyEnemyMaxHealth = 10f;
    [SerializeField] private float easyEnemyDamageToPlayer = 1f;

    [SerializeField] private float easyPlayerMaxHp = 140f;
    [SerializeField] private float easyPlayerDamageToEnemy = 15f;

    [SerializeField] private float easyHpDrainPerSecond = 0.20f;
    [SerializeField] private float easyStepPenalty = -0.0001f;
    [SerializeField] private float easyWaterStepPenalty = 0f;

    [Header("Optional shaping ramps")]
    [SerializeField] private bool scaleEnemyBrain = true;
    [SerializeField] private float easyEnemyAttackCooldown = 5.0f;
    [SerializeField] private float easyEnemyMoveSpeed = 1.0f;

    [Header("Anti-collapse shaping (IMPORTANT)")]
    [Tooltip("Enemy difficulty scales non-linearly: tEnemy = t^power. >1 means slower early ramp.")]
    [SerializeField] private float enemyCurvePower = 2.2f;

    [Tooltip("Drain scales slightly non-linearly to avoid drowning reward.")]
    [SerializeField] private float drainCurvePower = 1.4f;

    [Tooltip("Clamp step penalty so it can't dominate in later lessons.")]
    [SerializeField] private float maxNegativeStepPenalty = -0.0002f;

    [Tooltip("Clamp water step penalty so it can't dominate in later lessons.")]
    [SerializeField] private float maxNegativeWaterStepPenalty = -0.00025f;

    [Tooltip("Scale wood reward with difficulty to keep farming relevant at high difficulty.")]
    [SerializeField] private bool scaleWoodRewardWithDifficulty = true;

    [Tooltip("At max difficulty, wood pickup reward will be multiplied by this factor.")]
    [SerializeField] private float woodRewardMultiplierAtMax = 2f;

    public enum SpawnTier { Easy, Mid, Hard }

    public SpawnTier GetSpawnTier()
    {
        float d = GetEffectiveDifficultyRaw();
        if (d < 3f) return SpawnTier.Easy;
        if (d < 7f) return SpawnTier.Mid;
        return SpawnTier.Hard;
    }
    private float GetEffectiveDifficultyRaw()
    {
        // En training headless/build, on ne veut JAMAIS l override inspector
        if (Application.isBatchMode)
        {
            return Academy.Instance.EnvironmentParameters.GetWithDefault(difficultyParamName, difficultyMin);
        }

        return useInspectorDifficulty
            ? inspectorDifficulty
            : Academy.Instance.EnvironmentParameters.GetWithDefault(difficultyParamName, difficultyMin);
    }
    private void Awake()
    {
        if (resetManager == null) resetManager = GetComponent<EpisodeResetManager>();
        if (player == null) player = FindFirstObjectByType<PlayerAgent>();

        if (baseConfigAsset == null)
        {
            Debug.LogError("[DifficultyCurriculumMapper] Missing baseConfigAsset reference.", this);
            return;
        }

        runtimeConfig = Instantiate(baseConfigAsset);
        runtimeConfig.name = baseConfigAsset.name + "_RUNTIME";
        runtimeConfig.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

        if (resetManager != null) resetManager.SetConfig(runtimeConfig);
        if (player != null) player.config = runtimeConfig;

        ApplyNow();
    }
   
    public void ApplyNow()
    {
        if (runtimeConfig == null || baseConfigAsset == null) return;

        float d = GetEffectiveDifficultyRaw();
        //Debug.Log($"[CurriculumMapper] difficultyParam='{difficultyParamName}' value={d} (default={difficultyMin})");

        float t = Mathf.InverseLerp(difficultyMin, difficultyMax, d);
        t = Mathf.Clamp01(t);

        ApplyMapping(t);
        //Debug.Log($"d={d} t01={t} enemySpawnChance={runtimeConfig.enemySpawnChancePerTree}");
 
        runtimeConfig.ValidateRuntime();
    }

    private void ApplyMapping(float t01)
    {
        // Split curves: enemies ramp slower than "general progression"
        float tEnemy = Mathf.Pow(t01, Mathf.Max(0.01f, enemyCurvePower));
        float tDrain = Mathf.Pow(t01, Mathf.Max(0.01f, drainCurvePower));

        // --- Goals / player ---
        runtimeConfig.targetWood = Mathf.RoundToInt(Mathf.Lerp(easyTargetWood, baseConfigAsset.targetWood, t01));
        runtimeConfig.targetWood = Mathf.Max(1, runtimeConfig.targetWood);

        runtimeConfig.playerMaxHp = Mathf.Lerp(easyPlayerMaxHp, baseConfigAsset.playerMaxHp, t01);
        runtimeConfig.playerDamageToEnemy = Mathf.Lerp(easyPlayerDamageToEnemy, baseConfigAsset.playerDamageToEnemy, t01);

        // --- Costs (anti-drowning clamps) ---
        runtimeConfig.hpDrainPerSecond = Mathf.Lerp(easyHpDrainPerSecond, baseConfigAsset.hpDrainPerSecond, tDrain);

        runtimeConfig.stepPenalty = Mathf.Lerp(easyStepPenalty, baseConfigAsset.stepPenalty, t01);
        runtimeConfig.stepPenalty = Mathf.Max(runtimeConfig.stepPenalty, maxNegativeStepPenalty);

        runtimeConfig.waterStepPenalty = Mathf.Lerp(easyWaterStepPenalty, baseConfigAsset.waterStepPenalty, t01);
        runtimeConfig.waterStepPenalty = Mathf.Max(runtimeConfig.waterStepPenalty, maxNegativeWaterStepPenalty);

        // --- Enemies (non-linear ramp) ---
        runtimeConfig.enemySpawnChancePerTree = Mathf.Lerp(easyEnemySpawnChancePerTree, baseConfigAsset.enemySpawnChancePerTree, tEnemy);
        runtimeConfig.enemyMaxHealth = 1f;
        runtimeConfig.enemyDamageToPlayer = Mathf.Lerp(easyEnemyDamageToPlayer, baseConfigAsset.enemyDamageToPlayer, tEnemy);

        if (scaleEnemyBrain)
        {
            runtimeConfig.enemyAttackCooldown = Mathf.Lerp(easyEnemyAttackCooldown, baseConfigAsset.enemyAttackCooldown, tEnemy);
            runtimeConfig.enemyMoveSpeed = Mathf.Lerp(easyEnemyMoveSpeed, baseConfigAsset.enemyMoveSpeed, tEnemy);
        }
        else
        {
            runtimeConfig.enemyAttackCooldown = baseConfigAsset.enemyAttackCooldown;
            runtimeConfig.enemyMoveSpeed = baseConfigAsset.enemyMoveSpeed;
        }

        float dEff = Mathf.Clamp(GetEffectiveDifficultyRaw(), difficultyMin, difficultyMax);

        // hard code pour s'assurer qu'il n'y a pas d'erreur
        if (dEff < 5.0f) runtimeConfig.enemySpawnChancePerTree = 0f;

        // --- Rewards (prevent farming from becoming irrelevant) ---
        runtimeConfig.rewardWin = baseConfigAsset.rewardWin;
        runtimeConfig.penaltyDeath = baseConfigAsset.penaltyDeath;

        float woodReward = baseConfigAsset.rewardPerWoodPickup;
        if (scaleWoodRewardWithDifficulty)
        {
            float m = Mathf.Lerp(1f, Mathf.Max(1f, woodRewardMultiplierAtMax), t01);
            woodReward *= m;
        }
        runtimeConfig.rewardPerWoodPickup = woodReward;

        runtimeConfig.rewardEnemyHit = baseConfigAsset.rewardEnemyHit;
        runtimeConfig.rewardEnemyKill = baseConfigAsset.rewardEnemyKill;
        runtimeConfig.maxRewardedHitsPerEnemy = baseConfigAsset.maxRewardedHitsPerEnemy;

        // Preserve authored constants
        runtimeConfig.maxStep = baseConfigAsset.maxStep;
        runtimeConfig.moveSpeed = baseConfigAsset.moveSpeed;
        runtimeConfig.rotateSpeedDegPerSec = baseConfigAsset.rotateSpeedDegPerSec;
        runtimeConfig.slashCooldownSeconds = baseConfigAsset.slashCooldownSeconds;
        runtimeConfig.playerHitboxActiveSeconds = baseConfigAsset.playerHitboxActiveSeconds;

        runtimeConfig.enemyTurnSpeedDegPerSec = baseConfigAsset.enemyTurnSpeedDegPerSec;
        runtimeConfig.enemySpawnOffsetRadius = baseConfigAsset.enemySpawnOffsetRadius;
    }
}