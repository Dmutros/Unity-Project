using UnityEngine;

public class HealthComponent : MonoBehaviour
{
    public float maxHealth = 100;
    public float defense = 0;
    public float regenRate = 2;

    public float immunityTime = 1;
    private float lastDamageTime = -999f;

    public HealthData Health { get; private set; }

    public GameObject damagePopupPrefab;
    public GameObject healthBarPrefab;
    private EnemyHealthBar healthBarEn;

    public SpriteRenderer spriteRenderer;
    public float flashDuration = 0.1f;
    public int flashCount = 3;
    private Coroutine flashCoroutine;

    private Vector2 knockbackDeathDir;
    private float knockbackDeathForce;

    private void Awake()
    {
        Health = new HealthData(maxHealth, defense, regenRate);

        if (healthBarPrefab != null)
        {
            GameObject hb = Instantiate(healthBarPrefab, transform.position - new Vector3(0, 0.14f, 0), Quaternion.identity, transform);
            healthBarEn = hb.GetComponent<EnemyHealthBar>();
            healthBarEn.SetTarget(Health);
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Health.Regenerate(Time.deltaTime);

        if (Health.IsDead)
        {
            GetComponent<Death>().Die(knockbackDeathDir, knockbackDeathForce);
        }
    }

    public bool TakeDamage(float damage, Vector2 knockbackDir, float knockbackForce)
    {
        if (Time.time < lastDamageTime + immunityTime)
        {
            return false;
        }

        knockbackDeathDir = knockbackDir;
        knockbackDeathForce = knockbackForce;
        float minDamage = damage * 0.5f;
        float maxDamage = damage * 1.5f;
        float randomizedDamage = Random.Range(minDamage, maxDamage);

        float taken = Health.TakeDamage(randomizedDamage);
        ShowPopupDamage((int)taken);

        lastDamageTime = Time.time;

        FlashSprite();

        SendMessage("ApplyKnockback", new KnockbackData(knockbackDir, knockbackForce), SendMessageOptions.DontRequireReceiver);

        if (TryGetComponent<Enemy>(out Enemy enemy))
        {
            enemy.LookTowardsDirection(knockbackDir);
        }

        return true;
    }

    private void FlashSprite()
    {
        if (spriteRenderer == null) return;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashCoroutine());
    }

    private System.Collections.IEnumerator FlashCoroutine()
    {
        Color originalColor = spriteRenderer.color;
        Color transparentColor = originalColor;
        transparentColor.a = 0.5f;

        for (int i = 0; i < flashCount; i++)
        {
            spriteRenderer.color = transparentColor;
            yield return new WaitForSeconds(flashDuration);
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(flashDuration);
        }
    }

    private void ShowPopupDamage(int damage)
    {
        Vector3 spawnPosition = transform.position + new Vector3(Random.Range(-0.08f, 0.08f), 0.26f, 0);
        GameObject popup = Instantiate(damagePopupPrefab, spawnPosition, Quaternion.identity);
        PopupDamage pd = popup.GetComponent<PopupDamage>();
        pd.Setup(damage);
    }

    public bool IsInvunurable()
    {
        return Time.time < lastDamageTime + immunityTime;
    }
}
