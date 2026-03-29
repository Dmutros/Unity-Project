using UnityEngine;

public class MeleeSkeleton : WalkingEnemy
{
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private float dashForce = 0.5f;
    [SerializeField] private float dashDuration = 0.3f;
    [SerializeField] private float attackCooldown = 2f;
    [SerializeField] private float dashDamageMultiplier = 1.5f;
    [SerializeField] private float dashKnockbackMultiplier = 1.2f;

    private bool isDashing = false;
    private bool canAttack = true;
    private float dashTimer = 0f;
    private float attackCooldownTimer = 0f;
    private Vector2 dashDirection;
    private float originalMoveSpeed;

    private ContactDamage contactDamage;
    private bool isWindingUp = false;
    private float windUpDuration = 0.4f;
    private float windUpTimer = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
        originalMoveSpeed = moveSpeed;

        contactDamage = GetComponent<ContactDamage>();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (!canAttack)
        {
            attackCooldownTimer -= Time.deltaTime;
            if (attackCooldownTimer <= 0f)
            {
                canAttack = true;
            }
        }

        if (isWindingUp)
        {
            windUpTimer -= Time.deltaTime;

            if (windUpTimer <= 0f)
            {
                ExecuteDash();
                isWindingUp = false;
            }
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
            {
                EndDash();
            }
        }
    }
    protected override void FixedUpdate()
    {
        if (isDashing)
        {
            rb.velocity = new Vector2(dashDirection.x * dashForce, rb.velocity.y);
            return;
        }

        if (isWindingUp)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }
        base.FixedUpdate();
    }

    protected override void ChasingState()
    {
        if (targetPlayer != null && canAttack && !isDashing && !isWindingUp)
        {
            float distanceToPlayer = Vector2.Distance(transform.position, targetPlayer.position);

            if (distanceToPlayer <= attackRange && onGround && Mathf.Abs(transform.position.y - targetPlayer.position.y) <= blockSize)
            {
                StartAttack();
                return;
            }
        }
        base.ChasingState();
    }

    private void StartAttack()
    {
        canAttack = false;
        attackCooldownTimer = attackCooldown;

        isWindingUp = true;
        windUpTimer = windUpDuration;

        Vector2 directionToPlayer = (targetPlayer.position - transform.position).normalized;
        dashDirection = new Vector2(directionToPlayer.x, 0);

        spriteRend.flipX = dashDirection.x < 0;
    }

    private void ExecuteDash()
    {
        isDashing = true;
        dashTimer = dashDuration;

        if (contactDamage != null)
        {
            contactDamage.damageAmount *= dashDamageMultiplier;
            contactDamage.knockbackForce *= dashKnockbackMultiplier;
        }

    }

    private void EndDash()
    {
        isDashing = false;
        dashTimer = 0;

        if (contactDamage != null)
        {
            contactDamage.damageAmount /= dashDamageMultiplier;
            contactDamage.knockbackForce /= dashKnockbackMultiplier;
        }

        rb.velocity = new Vector2(rb.velocity.x * 0.5f, rb.velocity.y);

    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);


        if (isWindingUp)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, dashDirection * dashForce * 0.1f);
        }
    }
}
