using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HarvestTree : MonoBehaviour
{
    private Collider treeCollider;

    // le maxWood est également la vie de l'arbre
    [SerializeField] private int maxWood = 3;
    [SerializeField] private int woodPerHit = 1;

    [Header("Drops")]
    [SerializeField] private GameObject woodDropPrefab;
    [SerializeField] private float dropRadius = 1.75f;
    [SerializeField] private float ySpawnOffset = 0.25f;

    public int WoodRemaining { get; private set; }
    public bool IsDepleted { get; private set; }

    private void Awake()
    {
        treeCollider = GetComponent<Collider>();
        
        if (woodDropPrefab == null)
            Debug.LogError("woodDropPrefab is missing.");
    }

    public void ApplyConfig(PlayerAgentConfig cfg)
    {
        if (cfg == null) return;

        maxWood = cfg.treeMaxWood;
        woodPerHit = cfg.treeWoodPerHit;
        dropRadius = cfg.treeDropRadius;
        ySpawnOffset = cfg.treeDropYOffset;
    }

    private void Start()
    {
        ResetTreeToFull();
    }

    public void ResetTreeToFull()
    {
        IsDepleted = false;
        WoodRemaining =  maxWood;
        treeCollider.enabled = true;
    }

    /// <summary>
    /// drop le wood prefab lorsque le player hit l'arbre
    /// fonction qui comunique avec player
    /// </summary>
    public void ApplyHit()
    {
        if (IsDepleted) return;

        int taken = woodPerHit;
        DropWood(taken);

        WoodRemaining -= taken;

        if (WoodRemaining <= 0)
            DepleteAndDisable();
    }

    private void DropWood(int count)
    {
        if (count <= 0) return;

        //Normalement une seul itértion, car 1 woodPerhit
        for (int i = 0; i < count; i++)
        {
            Vector2 r = Random.insideUnitCircle * dropRadius;
            Vector3 pos = transform.position + new Vector3(r.x, ySpawnOffset, r.y);

            // on veut que le EpisodeManager commande cela pour centraliser 
            if (EpisodeResetManager.Instance != null)
                EpisodeResetManager.Instance.SpawnDrop(woodDropPrefab, pos, Quaternion.identity);

            else // si l'épisode manager n'est pas actif
                Instantiate(woodDropPrefab, pos, Quaternion.identity);
        }
    }

    private void DepleteAndDisable()
    {
        IsDepleted = true;
        treeCollider.enabled = false;
    }
}
