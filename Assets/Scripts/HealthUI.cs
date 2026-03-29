using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public Slider healthBar;
    public TextMeshProUGUI healthText;

    private HealthData health;

    public void SetHealthTarget(HealthData data)
    {
        health = data;
        healthBar.maxValue = data.MaxHealth;
    }
    void Update()
    {
        if (health != null)
        {
            healthBar.value = health.CurrentHealth;
            healthText.text = $"{health.CurrentHealth:F0} / {health.MaxHealth}";
        }
    }
}
