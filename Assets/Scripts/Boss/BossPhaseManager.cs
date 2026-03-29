using UnityEngine;

public class BossPhaseManager : MonoBehaviour
{
    private BossPhase currentPhase;
    [SerializeField] private GameObject wavePrefab;
    [SerializeField] private GameObject spitPrefab;

    [SerializeField] private GameObject spitBurstPrefab;
    [SerializeField] private GameObject bombPrefab;

    [SerializeField] private GameObject lineIndicatorPrefab;
    [SerializeField] private GameObject insectProjectilePrefab;

    private HealthComponent health;
    private bool hasEnteredFlyingPhase = false;

    private BossHealthUI bossHealthUI;

    private Rigidbody2D rb;

    public float maxFallSpeed = -5f;

    void Start()
    {
        health = GetComponent<HealthComponent>();
        SetPhase(new PhaseGroundOnly(gameObject, wavePrefab, spitPrefab));

        bossHealthUI = FindObjectOfType<BossHealthUI>();
        if (bossHealthUI != null)
        {
            bossHealthUI.SetHealthComponent(health);
            bossHealthUI.phaseManager = this;
        }

        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (!hasEnteredFlyingPhase && health.Health.CurrentHealth <= health.Health.MaxHealth * 0.6f)
        {
            hasEnteredFlyingPhase = true;

            SetPhase(new PhaseFlyingAndGround(gameObject, wavePrefab, spitPrefab, spitBurstPrefab, bombPrefab, lineIndicatorPrefab, insectProjectilePrefab));
        }
        currentPhase?.UpdatePhase();
    }
    private void FixedUpdate()
    {
        if (rb.velocity.y < maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, maxFallSpeed);
        }
    }
    public void SetPhase(BossPhase newPhase)
    {
        currentPhase?.ExitPhase();
        currentPhase = newPhase;
        currentPhase.EnterPhase();
    }
    public string GetCurrentPhaseName()
    {
        if (hasEnteredFlyingPhase)
            return "Phase 2";
        else
            return "Phase 1";
    }
}


