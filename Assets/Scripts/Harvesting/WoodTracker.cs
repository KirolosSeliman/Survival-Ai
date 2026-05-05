using System;
using UnityEngine;

public class WoodTracker : MonoBehaviour
{
    [SerializeField] private int targetWood = 10;

    public event Action<int, int> OnWoodCollected; // (ancienne Valeur, nouvelle Valeur)
    public event Action OnReachedTarget;

    public int TargetWood => targetWood;
    public int CollectedWood { get; private set; }
    public bool ReachedTargetWood => CollectedWood >= targetWood;

    public void SetTarget(int target)
    {
        // si ce n'est pas bon, par défaut c'est 10
        if (target <= 0) target = 10;  

        bool wasReached = ReachedTargetWood;
        targetWood = target;

        if (!wasReached && ReachedTargetWood)
            OnReachedTarget?.Invoke();
    }

    public void AddCollectedWood(int addedAmount)
    {
        if (addedAmount <= 0) return;
        if (ReachedTargetWood) return;

        bool wasReached = ReachedTargetWood;

        int oldValue = CollectedWood;
        CollectedWood = oldValue + addedAmount;

        OnWoodCollected?.Invoke(oldValue, CollectedWood);

        if (!wasReached && ReachedTargetWood)
            OnReachedTarget?.Invoke();
    }

    public void ResetProgress()
    {
        int oldValue = CollectedWood;
        CollectedWood = 0;
        OnWoodCollected?.Invoke(oldValue, CollectedWood);
    }
}
