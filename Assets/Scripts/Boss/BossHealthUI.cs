using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthUI : MonoBehaviour
{
    public Slider healthBar;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI phaseText;

    public HealthData bossHealth;
    public BossPhaseManager phaseManager;

    public GameObject healthBarP;

    private void Start()
    {
        healthBarOff(false);
        if (bossHealth != null)
        {
            healthBar.maxValue = bossHealth.MaxHealth;
        }

        if (phaseManager == null)
            phaseManager = FindObjectOfType<BossPhaseManager>();

        UpdatePhaseName();
    }


    private void Update()
    {
        if (bossHealth == null) return;

        healthBar.value = bossHealth.CurrentHealth;

        if (healthText != null)
        {
            healthText.text = $"{bossHealth.CurrentHealth:F0} / {bossHealth.MaxHealth:F0}";
        }

        UpdatePhaseName();
    }

    private void UpdatePhaseName()
    {
        if (phaseText != null && phaseManager != null)
        {
            string phaseName = phaseManager.GetCurrentPhaseName();
            Debug.Log("Phase name: " + phaseName);
            phaseText.text = phaseName;
        }
        else
        {
            Debug.LogWarning("phaseText або phaseManager == null");
        }
    }

    public void SetHealthComponent(HealthComponent component)
    {
        if (component == null) return;

        bossHealth = component.Health;
        healthBar.maxValue = bossHealth.MaxHealth;
        healthBarOff(true);
    }

    public void healthBarOff(bool value)
    {
        healthBarP.SetActive(value);
    }
}
