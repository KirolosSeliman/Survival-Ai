using UnityEngine;

/// <summary>
/// Cette composante ce met sur le prefab du wood qui spawn de l'arbre
/// permet de destroy le game object qui l'utilise quand le wood se met dans l'inventaire
/// permet de facilement compter de maniere individuelle le wood qui tombe
/// </summary>

[RequireComponent (typeof(Collider))]
public class DropedWood : MonoBehaviour
{
    [Header("Nombre de wood par prefab, doit etre égale HarvestTree woodPerHit")]
    [SerializeField] private int amount = 1;

    
    [SerializeField] private string collectorTag = "Player";

    private bool collected;
    private Collider c;

    private void Awake()
    {
        Debug.Assert(amount > 0);

         c = GetComponent<Collider>();

        // assure au début qu'il est trigger, car on dépend de cela
        if (!c.isTrigger) c.isTrigger = true;
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (!other.CompareTag(collectorTag)) return;

        // le wood tracker component est sur le player
        WoodTracker tracker = other.GetComponentInParent<WoodTracker>();

        if (tracker == null)  return;
     
        collected = true;

        tracker.AddCollectedWood(amount);
       
        // Durant l'entrainement de l'agent on enleve les lignes de log, car devient lourds au fil du temps pour la machine
        //Debug.Log($"[DropedWood] +{amount} wood collected. Total={tracker.CollectedWood}/{tracker.TargetWood}", tracker);

        Destroy(gameObject);
    }
}
