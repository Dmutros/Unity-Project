using System;
using TMPro;
using UnityEngine;

public class CharacterScript : MonoBehaviour
{
    public float moveSpeed;
    public float jumpForce;
    public bool onGround;

    public float horizontal;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRend;
    private Animator animator;

    [SerializeField] private LayerMask groundLayer;

    private bool isStepping = false;
    private float stepTargetY;
    private float stepSpeed = 3f;
    public float stepCooldown = 1f;
    private float stepCooldownTimer = 0f;

    public float maxFallSpeed = -5f;

    private float horizDup;

    public HealthUI healthUI;

    private bool isKnockedBack = false;
    private float knockbackDuration = 0.2f;
    private float knockbackTimer = 0f;

    public Camera mainCamera;
    bool isAttacking;

    private float fallStartY;
    private float highestYDuringFall;
    private bool isFalling = false;

    public float fallDamageThreshold = 1.2f;
    public float fallDamageMultiplier = 50f;

    private bool wasOnGroundLastFrame = true;

    public AudioClip swordSlash;
    public AudioSource audioSource;

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            onGround = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & groundLayer) != 0)
        {
            onGround = false;
        }
    }

    public void ApplyKnockback(object data)
    {
        KnockbackData kb = (KnockbackData)data;

        rb.velocity = Vector2.zero;
        rb.AddForce(kb.direction.normalized * kb.force, ForceMode2D.Impulse);

        isKnockedBack = true;
        knockbackTimer = knockbackDuration;
    }

    public void SetAttacking(bool value)
    {
        isAttacking = value;
    }

    public void SwingSound()
    {
        audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(swordSlash);
    }

    private void FixedUpdate()
    {
        if (isKnockedBack)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
            }
            return;
        }

        horizontal = Input.GetAxis("Horizontal");

        float xVelocity = horizontal * moveSpeed;

        float yVelocity = rb.velocity.y;

        if (yVelocity < maxFallSpeed)
        {
            yVelocity = maxFallSpeed;
        }

        rb.velocity = new Vector2(xVelocity, yVelocity);

        if (!onGround && !isFalling && rb.velocity.y < -0.1f)
        {
            isFalling = true;
            fallStartY = transform.position.y;
        }

        if (onGround && isFalling)
        {
            float fallDistance = fallStartY - transform.position.y;

            if (fallDistance > fallDamageThreshold)
            {
                int damage = Mathf.RoundToInt((fallDistance - fallDamageThreshold) * fallDamageMultiplier);
                var health = GetComponent<HealthComponent>();
                if (health != null)
                {
                    health.TakeDamage(damage, Vector2.up, 0f);
                    Debug.Log($"Fall damage: {damage} from distance: {fallDistance:F2}");
                }
            }

            isFalling = false;
        }


        if (onGround && horizontal != 0)
        {
            if (!isAttacking)
            {
                Flip(horizontal < 0);
            }
            AutoJump();
        }

        //Jump
        float jump = Input.GetAxis("Jump");

        if (jump != 0)
        {
            if (onGround)
            {
                rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            }
        }
        //Jump end

        //float vertical = Input.GetAxis("Vertical");

    }

    public void Flip(bool value)
    {
        spriteRend.flipX = value;
    }

    private void AutoJump()
    {
        Vector2 direction = new Vector2(horizDup, 0);

        float checkDistance = 0.12f;
        float checkDistVert = 0.16f;
        float forwardOffset = 0.12f;

        Vector2 bottomPos = new Vector2(transform.position.x, transform.position.y - 0.08f);
        Vector2 vertOrigin = new Vector2(transform.position.x + (horizontal > 0 ? forwardOffset : -forwardOffset), transform.position.y + 0.16f);
        Vector2 vertDirection = Vector2.down;

        RaycastHit2D lowHit = Physics2D.Raycast(bottomPos, direction, checkDistance, groundLayer);
        RaycastHit2D vertHit = Physics2D.Raycast(vertOrigin, Vector2.down, checkDistVert, groundLayer);

        Debug.DrawRay(bottomPos, direction * checkDistance, Color.red);
        Debug.DrawRay(vertOrigin, Vector2.down * checkDistVert, Color.magenta);

        if (lowHit.collider != null && vertHit.collider == null && stepCooldownTimer <= 0)
        {
            stepTargetY = transform.position.y + 0.3f;
            isStepping = true;
            stepCooldownTimer = stepCooldown;
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRend = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        HealthComponent health = GetComponent<HealthComponent>();
        if (health != null && healthUI != null)
        {
            healthUI.SetHealthTarget(health.Health);
        }
    }

    // Update is called once per frame
    void Update()
    {

        animator.SetFloat("Horizontal", MathF.Abs(horizontal));

        animator.SetBool("onGround", onGround);

        if (horizontal > 0)
        {
            horizDup = 1f;
        }
        else if (horizontal < 0)
        {
            horizDup = -1f;
        }

        if (stepCooldownTimer > 0)
        {
            stepCooldownTimer -= Time.deltaTime;
        }

        if (onGround)
        {
            if (isStepping)
            {
                float newY = Mathf.MoveTowards(transform.position.y, stepTargetY, stepSpeed * Time.deltaTime);
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);

                isStepping = false;
            }
        }
    }
    public bool GetIsAttacking()
    {
        return isAttacking;
    }

}
