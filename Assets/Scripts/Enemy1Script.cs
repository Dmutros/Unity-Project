using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

public class Enemy1Script : MonoBehaviour
{
    public float moveSpeed;
    public float jumpForce;
    public float jumpForceEz;
    public bool onGround;

    public float horizontal;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRend;
    private Animator animator;

    [SerializeField] private LayerMask groundLayer;

    private int rayCount = 5;
    private float visionDist = 5f;
    private float visionAngle = 45f;
    private float horizDup;

    private bool hasTriedJump = false;
    private float beforeJumpX;
    private float jumpTimer = 0;
    public float jumpDelay = 0.5f;

    private Vector2 jumpStartPos;

    private bool isStepping = false;
    private float stepTargetY;
    private float stepSpeed = 3f;
    public float stepCooldown = 1f;
    private float stepCooldownTimer = 0;

    RaycastHit2D[] hits;
    private Transform targetPlayer;
    private Vector2 lastSeenPos;
    private float timeSinceSeen = 0;
    public float lostSightCooldown = 3f;
    private bool isLookingAround = false;
    private float lookAroundTimer = 2f;
    private float currentLookTimer = 0;
    private int lookDirection = 1;



    private enum GroundResult
    {
        GroundClose,
        GroundFar,
        NoGround
    }

    private enum EnemyState
    {
        Patrolling,
        Chasing,
        Searching
    }
    private EnemyState currentState = EnemyState.Patrolling;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRend = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        ChooseDirection();

        InvokeRepeating(nameof(ChooseDirection), 3f, Random.Range(3f, 30f));
    }
    private void FixedUpdate()
    {
        switch (currentState)
        {
            case EnemyState.Patrolling:
                PatrollingState();
                break;

            case EnemyState.Chasing:
                ChasingState();
                break;

            case EnemyState.Searching:
                SearchingState();
                break;
        }
        if (onGround)
        {
          
            if (horizontal != 0)
            {
                spriteRend.flipX = horizontal < 0;
                DetectJumpOpportunity();
            }
        }

    }

    // Update is called once per frame
    void Update()
    {
        //animator hee
        Vision();

        if (hasTriedJump)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0)
            {
                float deltaX = Mathf.Abs(transform.position.x - beforeJumpX);
                if (deltaX < 0.1f)
                {
                    horizontal *= -1f;
                }
                hasTriedJump = false;
            }
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

                if (Mathf.Approximately(newY, stepTargetY))
                {
                    isStepping = false;
                }
                isStepping = false;
            }
        }

    }

    void ChooseDirection()
    {
        int rand = Random.Range(0, 3);
        horizontal = rand == 0 ? -1f : (rand == 1 ? 0f : 1f);
    }

    private void Vision()
    {

        if (horizontal > 0)
        {
            horizDup = 1f;
        }
        else if (horizontal < 0)
        {
            horizDup = -1f;
        }

        Vector2 baseDirection = new Vector2(horizDup, 0).normalized;

        int half = rayCount / 2;

        for (int i = -half; i <= half; i++)
        {
            float angleStep = visionAngle / rayCount;
            float angle = angleStep * i;

            Vector2 rayDirection = Quaternion.Euler(0, 0, angle) * baseDirection;

            hits = Physics2D.RaycastAll(transform.position, rayDirection, visionDist);

            Debug.DrawRay(transform.position, rayDirection * visionDist, Color.yellow);

            foreach (var hit in hits)
            {
                Debug.Log("Промінь влучив в: " + hit.collider.name);



                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground"))
                {
                    Debug.Log("Бачення блокує земля");
                    break;
                }

                if (hit.collider.CompareTag("Player"))
                {
                    Debug.Log("Бачу гравця!");
                    currentState = EnemyState.Chasing;
                    targetPlayer = hit.collider.transform;
                    lastSeenPos = targetPlayer.position;
                    timeSinceSeen = 0f;
                    isLookingAround = false;
                    break;
                }
            }
        }
    }

    private void DetectJumpOpportunity()
    {
        Vector2 direction = new Vector2(horizontal, 0);
        float checkDistance = 0.12f;
        float checkDistVert = 0.16f;
        float forwardOffset = 0.12f;

        Vector2 bottomPos = new Vector2(transform.position.x, transform.position.y - 0.1f);
        Vector2 middlePos = new Vector2(transform.position.x, transform.position.y);
        Vector2 topPos = new Vector2(transform.position.x, transform.position.y + 0.1f);
        Vector2 vertOrigin = new Vector2(transform.position.x + (horizontal > 0 ? forwardOffset : -forwardOffset), transform.position.y + 0.16f);
        Vector2 vertDirection = Vector2.down;

        RaycastHit2D lowHit = Physics2D.Raycast(bottomPos, direction, checkDistance, groundLayer);
        RaycastHit2D midHit = Physics2D.Raycast(middlePos, direction, checkDistance, groundLayer);
        RaycastHit2D topHit = Physics2D.Raycast(topPos, direction, checkDistance, groundLayer);
        RaycastHit2D vertHit = Physics2D.Raycast(vertOrigin, vertDirection, checkDistVert, groundLayer);

        Debug.DrawRay(bottomPos, direction * checkDistance, Color.red);
        Debug.DrawRay(middlePos, direction * checkDistance, Color.green);
        Debug.DrawRay(topPos, direction * checkDistance, Color.blue);
        Debug.DrawRay(vertOrigin, vertDirection * checkDistVert, Color.magenta);

        if (lowHit.collider != null && vertHit.collider == null && stepCooldownTimer <= 0)
        {
            stepTargetY = transform.position.y + 0.2f;
            isStepping = true;
            stepCooldownTimer = stepCooldown;
        }
        else if (midHit.collider != null && topHit.collider == null)
        {
            if (!hasTriedJump)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                beforeJumpX = transform.position.x;
                hasTriedJump = true;
                jumpTimer = jumpDelay;

            }
        }
        else if (topHit.collider != null)
        {
            if (!hasTriedJump)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                beforeJumpX = transform.position.x;
                hasTriedJump = true;
                jumpTimer = jumpDelay;

            }

        }

    }

    private GroundResult IsGroundAhead()
    {
        float checkDistDiag = 1f;
        float checkDistVert = 1f;
        float offsetY = 0.1f;
        float forwardOffset = 0.18f;

        //Diagonal ray
        Vector2 diagOrigin = new Vector2(transform.position.x, transform.position.y - offsetY);
        Vector2 diagDirection = horizontal > 0
        ? new Vector2(1f, -0.5f).normalized
        : new Vector2(-1f, -0.5f).normalized;

        RaycastHit2D hit = Physics2D.Raycast(diagOrigin, diagDirection, checkDistDiag, groundLayer);
        Debug.DrawRay(diagOrigin, diagDirection * checkDistDiag, Color.cyan);

        //Vertical ray
        Vector2 vertOrigin = new Vector2(transform.position.x + (horizontal > 0 ? forwardOffset : -forwardOffset), transform.position.y);
        Vector2 vertDirection = Vector2.down;

        RaycastHit2D vertHit = Physics2D.Raycast(vertOrigin, vertDirection, checkDistVert, groundLayer);
        Debug.DrawRay(vertOrigin, vertDirection * checkDistVert, Color.magenta);

        ///////////////////////////
        if (vertHit.collider == null)
        {
            if (hit.collider != null)
            {
                float dist = hit.distance;

                if (dist < 0.2f)
                {
                    return GroundResult.GroundClose;
                }
                else if (dist > 0.2f)
                {
                    return GroundResult.GroundFar;
                }
            }
            return GroundResult.NoGround;
        }
        else
        {
            return GroundResult.GroundClose;
        }

    }

    private void PatrollingState()
    {
        if (onGround)
        {
            switch (IsGroundAhead())
            {
                case GroundResult.GroundClose:
                    rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y); break;

                case GroundResult.GroundFar:
                    rb.velocity = new Vector2(rb.velocity.x, jumpForce); break;

                case GroundResult.NoGround:
                    horizontal *= -1; break;
            }

        }
        else
        {
            rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        }

    }

    private void ChasingState()
    {
        if (targetPlayer == null)
        {
            currentState = EnemyState.Patrolling;
            return;
        }
        else
        {
            MoveTowards(targetPlayer.position);
        }

        timeSinceSeen += Time.deltaTime;

        rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        spriteRend.flipX = horizontal < 0;


        if (timeSinceSeen > lostSightCooldown)
        {
            currentState = EnemyState.Searching;
            isLookingAround = false;
        }
    }

    private void SearchingState()
    {
        float dist = Vector2.Distance(transform.position, lastSeenPos);

        if (!isLookingAround)
        {
            MoveTowards(lastSeenPos);
            if (dist < 0.3f)
            {
                isLookingAround = true;
                currentLookTimer = lookAroundTimer;
                horizontal = 0;
            }
            rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);

        }
        else
        {
            currentLookTimer -= Time.deltaTime;
            if (currentLookTimer > lookAroundTimer / 2f)
            {
                spriteRend.flipX = true;
                horizDup = -1f;
            }
            else
            {
                spriteRend.flipX = false;
                horizDup = 1f;
            }

            if (currentLookTimer <= 0)
            {
                currentState = EnemyState.Patrolling;
                ChooseDirection();
            }
        }
    }

    private void MoveTowards(Vector2 target)
    {
        float directionToPlayer = Mathf.Sign(target.x - transform.position.x);
        horizontal = directionToPlayer;
    }

}

