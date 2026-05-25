using UnityEngine;

public class PlayerHarvest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerAgent agent;
    [SerializeField] private PlayerAgentRefs refs;

    private Collider[] hits;

    private void Awake()
    {
        if (agent == null) agent = GetComponentInParent<PlayerAgent>();
        if (refs == null) refs = GetComponentInParent<PlayerAgentRefs>();

        if (agent == null) throw new MissingReferenceException("");
        if (refs == null) throw new MissingReferenceException("");
        if (agent.config == null) throw new MissingReferenceException("");

        agent.config.ValidateRuntime();
        hits = new Collider[Mathf.Max(8, agent.config.harvestMaxHits)];
    }

    public void ApplyConfig(PlayerAgentConfig cfg)
    {
        if (cfg == null) return;
        cfg.ValidateRuntime();

        int size = Mathf.Max(8, cfg.harvestMaxHits);
        if (hits == null || hits.Length != size)
            hits = new Collider[size];
    }

    public bool TryHarvest(out HarvestTree harvestedTree)
    {
        harvestedTree = null;

        var cfg = agent.config;
        if (cfg == null) return false;

        Transform playerT = refs.body != null ? refs.body : transform;
        Vector3 origin = playerT.position;

        int count = Physics.OverlapSphereNonAlloc(origin, cfg.harvestMaxDistance, hits, cfg.treeLayerMask, QueryTriggerInteraction.Collide);

        if (count <= 0) return false;

        HarvestTree chosen = cfg.harvestPreferClosestTree ? ChooseClosestValid(cfg, origin, count) : ChooseRandomValid(cfg, origin, count);

        if (chosen == null) return false;

        chosen.ApplyHit();
        harvestedTree = chosen;
        return true;
    }

    private HarvestTree ChooseClosestValid(PlayerAgentConfig cfg, Vector3 origin, int count)
    {
        HarvestTree best = null;
        float bestSqr = cfg.harvestMaxDistance * cfg.harvestMaxDistance;

        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i];
            if (c == null) continue;

            if (cfg.requireTreeTag && !c.CompareTag(cfg.treeTag)) continue;

            HarvestTree tree = c.GetComponentInParent<HarvestTree>();
            if (tree == null) continue;
            if (tree.IsDepleted) continue;

            float sqr = (tree.transform.position - origin).sqrMagnitude;
            if (sqr <= bestSqr)
            {
                bestSqr = sqr;
                best = tree;
            }
        }

        return best;
    }

    private HarvestTree ChooseRandomValid(PlayerAgentConfig cfg, Vector3 origin, int count)
    {
        HarvestTree chosen = null;
        int validCount = 0;
        float maxSqr = cfg.harvestMaxDistance * cfg.harvestMaxDistance;

        for (int i = 0; i < count; i++)
        {
            Collider c = hits[i];
            if (c == null) continue;

            if (cfg.requireTreeTag && !c.CompareTag(cfg.treeTag)) continue;

            HarvestTree tree = c.GetComponentInParent<HarvestTree>();
            if (tree == null) continue;
            if (tree.IsDepleted) continue;

            float sqr = (tree.transform.position - origin).sqrMagnitude;
            if (sqr > maxSqr) continue;

            validCount++;
            if (Random.Range(0, validCount) == 0)
                chosen = tree;
        }

        return chosen;
    }
}
