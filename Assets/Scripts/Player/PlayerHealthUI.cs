using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider healthBar;
    [SerializeField] private TMP_Text healthText;

    [Header("Source")]
    [SerializeField] private PlayerAgentRefs playerRefs; 

    private void Awake()
    {
        if (playerRefs == null)
            playerRefs = FindFirstObjectByType<PlayerAgentRefs>(FindObjectsInactive.Exclude);
    }

    private void Update()
    {
        float hp = playerRefs.hp;
        float maxHp = Mathf.Max(1f, playerRefs.maxHp);

        if (healthText != null) healthText.text = $"{hp:0} / {maxHp:0}";
        if (healthBar != null) healthBar.value = hp / maxHp;
    }
}
