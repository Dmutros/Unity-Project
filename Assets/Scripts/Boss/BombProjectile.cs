using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BombProjectile : MonoBehaviour
{
    public float explosionDelay = 2f;
    public float explosionRadius = 0.5f;
    public float damage = 25f;
    public float knockback = 5f;
    public GameObject explosionEffect;
    public GameObject smokeEffect;

    public float maxFallSpeed = -5f;
    private Rigidbody2D rb;
    private Animator animator;

    private SpriteRenderer spriteRenderer;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        animator = GetComponent<Animator>();

        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        if (rb.velocity.y < maxFallSpeed)
        {
            rb.velocity = new Vector2(rb.velocity.x, maxFallSpeed);
        }
    }

    private void Start()
    {
        StartCoroutine(SpeedUpAnimationRoutine());
        StartCoroutine(BlinkRedRoutine());
        Invoke(nameof(Explode), explosionDelay);
    }

    private void Explode()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                HealthComponent hc = hit.GetComponent<HealthComponent>();
                if (hc != null)
                {
                    Vector2 knockDir = (hit.transform.position - transform.position).normalized;
                    hc.TakeDamage(damage, knockDir, knockback);
                }
            }
        }

        if (explosionEffect != null)
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        if (smokeEffect != null)
            Instantiate(smokeEffect, transform.position, Quaternion.identity);


        Destroy(gameObject);
    }

    private IEnumerator SpeedUpAnimationRoutine()
    {
        float timer = 0f;
        float startSpeed = 1f;
        float endSpeed = 3f;

        while (timer < explosionDelay)
        {
            float t = timer / explosionDelay;
            animator.speed = Mathf.Lerp(startSpeed, endSpeed, t);
            timer += Time.deltaTime;
            yield return null;
        }

        animator.speed = 1f;
    }

    private IEnumerator BlinkRedRoutine()
    {
        float timer = 0f;
        Color baseColor = Color.white;
        Color blinkColor = Color.red;

        while (timer < explosionDelay)
        {
            float t = Mathf.PingPong(Time.time * 5f, 1f);
            spriteRenderer.color = Color.Lerp(baseColor, blinkColor, t);
            timer += Time.deltaTime;
            yield return null;
        }

        spriteRenderer.color = baseColor;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
