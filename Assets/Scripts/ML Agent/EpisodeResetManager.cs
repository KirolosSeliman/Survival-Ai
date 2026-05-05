using System.Collections.Generic;
using UnityEngine;

public class EpisodeResetManager : MonoBehaviour
{
    public static EpisodeResetManager Instance { get; private set; }

    [Header("Config (single source of truth at runtime)")]
    [SerializeField] private PlayerAgentConfig config;

    [Header("Curriculum (optional)")]
    [SerializeField] private DifficultyCurriculumMapper curriculumMapper;

    [Header("Player")]
    [SerializeField] private PlayerAgent player;

    [Header("Spawn Points (optional tiers)")]
    [SerializeField] private Transform[] easySpawns;
    [SerializeField] private Transform[] midSpawns;
    [SerializeField] private Transform[] hardSpawns;

    [Header("Enemies")]
    [SerializeField] private GameObject enemyPrefab;

    [Header("Runtime Organization")]
    [SerializeField] private Transform runtimeEnemiesRoot;
    [SerializeField] private Transform runtimeDropsRoot;
        
    [Header("Drop Grounding")]
    [SerializeField] private LayerMask dropGroundMask;   
    [SerializeField] private float dropGroundOffset = 0.05f;
    [SerializeField] private float dropRayUp = 2f;
    [SerializeField] private float dropRayDown = 10f;


    // le reset manager cache les postions
    private HarvestTree[] cachedTrees;
    private readonly List<GameObject> activeEnemies = new List<GameObject>(256);

    private void Awake()
    {
        if (Instance != null && Instance != this)
            throw new UnityException("");

        Instance = this;

        if (player == null) player = FindFirstObjectByType<PlayerAgent>();
        if (curriculumMapper == null) curriculumMapper = GetComponent<DifficultyCurriculumMapper>();

        if (config == null)
            throw new MissingReferenceException("");

        config.ValidateRuntime();

        // rend la hiarchie plus claire car les ennemies et les drops spawn en dessous  du game object avec le nom approprié
        if (runtimeEnemiesRoot == null) runtimeEnemiesRoot = CreateRoot("Runtime_Enemies");
        if (runtimeDropsRoot == null) runtimeDropsRoot = CreateRoot("Runtime_Drops");

        cachedTrees = FindObjectsByType<HarvestTree>(FindObjectsSortMode.None);
    }

    private Transform CreateRoot(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        return go.transform;
    }

    public void SetConfig(PlayerAgentConfig cfg)
    {
        config = cfg;
        if (config != null) config.ValidateRuntime();
    }

    public PlayerAgentConfig GetCurrentConfig() => config;

    public void ResetEnvironment()
    {
        if (player == null || config == null)
            throw new MissingReferenceException("player ou config maquant");

       // applique
        curriculumMapper?.ApplyNow();
        config.ValidateRuntime();

        // clear les vieux trucs
        DestroyChildren(runtimeEnemiesRoot);
        DestroyChildren(runtimeDropsRoot);
        activeEnemies.Clear();

    
        ResetAllTrees();

        ResetPlayer();

        SpawnEnemiesNearTrees();
    }

    private void ResetAllTrees()
    {
        if (cachedTrees == null || cachedTrees.Length == 0)
            cachedTrees = FindObjectsByType<HarvestTree>(FindObjectsSortMode.None);

        foreach (var tree in cachedTrees)
        {
            if (tree == null) continue;
            tree.ApplyConfig(config);
            tree.ResetTreeToFull();
        }
    }

    private void ResetPlayer()
    {
        var refs = player.GetComponent<PlayerAgentRefs>();
        if (refs == null)
            throw new MissingReferenceException("refs sont manquants");

        refs.maxHp = config.playerMaxHp;
        refs.hp = refs.maxHp;

        refs.slashCooldown?.SetCooldown(config.slashCooldownSeconds);

        if (refs.woodTracker != null)
        {
            refs.woodTracker.SetTarget(config.targetWood);
            refs.woodTracker.ResetProgress();
        }

        refs.harvest?.ApplyConfig(config);

        if (refs.rb != null)
        {
            refs.rb.linearVelocity = Vector3.zero;
            refs.rb.angularVelocity = Vector3.zero;
        }

        Transform spawn = PickSpawnPoint();
        // last resort 
        Vector3 pos = (spawn != null) ? spawn.position : Vector3.zero;
        Quaternion rot = (spawn != null) ? spawn.rotation : Quaternion.identity;

        if (spawn == null)
            Debug.LogWarning("[EpisodeResetManager] PickSpawnPoint() returned null. Using origin as fallback.", this);

        if (refs.rb != null)
        {
            refs.rb.position = pos;
            refs.rb.rotation = rot;
        }
        else
        {
            player.transform.SetPositionAndRotation(pos, rot);
        }
    }

    private Transform PickSpawnPoint()
    {
        var tier = DifficultyCurriculumMapper.SpawnTier.Easy;
        if (curriculumMapper != null)
            tier = curriculumMapper.GetSpawnTier();

        Transform[] set = tier switch
        {
            DifficultyCurriculumMapper.SpawnTier.Easy => easySpawns,
            DifficultyCurriculumMapper.SpawnTier.Mid => midSpawns,
            DifficultyCurriculumMapper.SpawnTier.Hard => hardSpawns,
            _ => easySpawns
        };

        if (set == null || set.Length == 0)
        {
            if (tier == DifficultyCurriculumMapper.SpawnTier.Hard)
                set = (midSpawns != null && midSpawns.Length > 0) ? midSpawns : easySpawns;
            else if (tier == DifficultyCurriculumMapper.SpawnTier.Mid)
                set = easySpawns;

            if (set == null || set.Length == 0)
            {
                Debug.LogError("[EpisodeResetManager] no spawn points configured (easy/mid/hard).", this);
                return null;
            }

            Debug.LogWarning($"[EpisodeResetManager] spawn tier '{tier}' empty, fallback used.", this);
        }

        return set[Random.Range(0, set.Length)];
    }

    private void SpawnEnemiesNearTrees()
    {
        if (enemyPrefab == null) return;
        if (cachedTrees == null || cachedTrees.Length == 0) return;

        foreach (var tree in cachedTrees)
        {
            if (tree == null) continue;

            if (Random.value > config.enemySpawnChancePerTree)
                continue;

            Vector3 basePos = tree.transform.position;
            Vector2 r = Random.insideUnitCircle * config.enemySpawnOffsetRadius;
            Vector3 spawnPos = new Vector3(basePos.x + r.x, basePos.y, basePos.z + r.y);

            GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity, runtimeEnemiesRoot);
            activeEnemies.Add(enemy);

            ConfigureSpawnedEnemy(enemy);
        }
    }

    private void ConfigureSpawnedEnemy(GameObject enemy)
    {
        if (enemy == null) return;

        enemy.GetComponent<EnemyMotorBehavior>()?.ApplyConfig(config);
        enemy.GetComponent<EnemyPerceptionScan>()?.ApplyConfig(config);
        enemy.GetComponent<EnemyBrainLogic>()?.ApplyConfig(config);
        enemy.GetComponentInChildren<EnemyAnimatorController>(true)?.ApplyConfig(config);
        enemy.GetComponentInChildren<EnemySwordHitBoxRelay>(true)?.ApplyConfig(config);

        var hp = enemy.GetComponent<EnemyHealth>();
        if (hp != null)
        {
            hp.SetMaxHealth(config.enemyMaxHealth);
            hp.SetPlayerAgent(player);
            hp.ResetToFull();
        }

        enemy.GetComponent<EnemyBrainLogic>()?.ForceDisableHitbox();
    }

    private Vector3 SpawnObjectToGround(Vector3 pos)
    {
        Vector3 origin = pos + Vector3.up * dropRayUp;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,dropRayUp + dropRayDown, dropGroundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.point + hit.normal * dropGroundOffset;
        }

        return pos; 
    }

    public GameObject SpawnDrop(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        if (prefab == null) return null;

        pos = SpawnObjectToGround(pos);

        GameObject wood = Instantiate(prefab, pos, rot, runtimeDropsRoot);

        // safety, hard code pour assurer que c'est bel et bien
        int droppedLayer = LayerMask.NameToLayer("DroppedWood");
        if (droppedLayer >= 0)
            wood.layer = droppedLayer;

        return wood;
    }

    private void DestroyChildren(Transform root)
    {
        if (root == null) return;

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Transform child = root.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    // a partir des cached tree 
    // origine c'est la position du player
    public Transform GetNearestTree(Vector3 origin, float maxDistance)
    {
        if (cachedTrees == null || cachedTrees.Length == 0) return null;

        float bestSqr = float.PositiveInfinity;
        Transform best = null;

        foreach (var tree in cachedTrees)
        {
            if (tree == null) continue;
            if (tree.IsDepleted) continue;

            float sqr = (tree.transform.position - origin).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = tree.transform;
            }
        }

        return best;
    }

    public Transform GetNearestEnemy(Vector3 origin, float maxDistance)
    {
        float bestSqr = maxDistance * maxDistance;
        Transform best = null;

        // Clean les null car a travers les milliers de run des fois il y a des erreurs,
        // alors il garde tout clean durant l'integralité de l'entrainement.
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null) activeEnemies.RemoveAt(i);
        }

        foreach (var e in activeEnemies)
        {
            if (e == null) continue;
            if (!e.activeInHierarchy) continue;

            float sqr = (e.transform.position - origin).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = e.transform;
            }
        }

        return best;
    }
}
