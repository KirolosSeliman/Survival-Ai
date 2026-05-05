using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class EnemyHealthUI : MonoBehaviour
{
    [SerializeField] private EnemyHealth target;
    [SerializeField] private Slider bar;
    [SerializeField] private TMP_Text label;

    private void OnEnable()
    {
        if (target != null)
        {
            target.OnHealthChanged += HandleChanged;
            HandleChanged(target.Current, target.Max);
        }
    }

    private void OnDisable()
    {
        if (target != null)
            target.OnHealthChanged -= HandleChanged;
    }

    private void HandleChanged(float current, float max)
    {
        float safeMax = Mathf.Max(1f, max);
        if (bar != null) bar.value = current / safeMax;
        if (label != null) label.text = $"{current:0}/{safeMax:0}";
    }
}
