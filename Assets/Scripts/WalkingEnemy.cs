using System.Collections.Generic;
using UnityEngine;

public class WalkingEnemy : Enemy
{
    // moving and behavior
    private float gravity = 9.81f;
    private float tAir;
    private float maxDistance;
    private float maxHeight;
    private bool isTrackingPlayer = false;

    // jumping
    private bool hasTriedJump = false;
    private float beforeJumpX;
    private float jumpTimer = 0;
    public float jumpDelay = 0.5f;
    private int maxJumpDistance;
    private int maxVerticalOffsetUp;
    private int maxVerticalUp;
    private int maxVerticalOffsetDown = 10;
    private float margin;

    // steps
    private bool isStepping = false;
    private float stepTargetY;
    private float stepSpeed = 3f;
    public float stepCooldown = 1f;
    private float stepCooldownTimer = 0;

    // platform search
    private bool platformSearching = false;
    private bool isJumpingToPlatform = false;
    private bool isDropingFromPlatform = false;
    private Vector3? worldTarget = null;
    private bool offsetPoint = true;
    private int consecutiveJumpAttempts = 0;
    private int consecutiveDirJumpAttempts = 0;
    private float lastJumpAttemptTime = 0f;
    private float jumpAttemptTimeout = 1f;
    private float platformApproachOffset = 0.0f;
    private int platformDirection = 0;
    private Vector2 targetPos;
    private Vector2 targetPos2;
    private float lastDirectJumpTime = 0f;
    private float tryDirectJumpCooldown = 0.8f;
    private float prevDirToTarg2 = float.MaxValue;
    private float lastCheck = 0f;
    private float lastTurn = 0f;

    // vision
    private float horizDup;
    private bool canSeePlayer = false;

    // search behavior
    private float searchingTime = 0;
    private bool hasStartedSearching = false;
    private float nextDirectionChangeTime;

    private enum GroundResult
    {
        GroundClose,
        GroundFar,
        JumpableSameLevel,
        JumpableUp,
        JumpableDown,
        NoGround
    }

    protected void OnValidate()
    {
        margin = Mathf.Clamp(jumpForce * 0.1f, 0.15f, 0.4f);

        tAir = (2f * jumpForce) / gravity;
        maxDistance = moveSpeed * tAir;

        maxHeight = (jumpForce * jumpForce) / (2f * gravity);

        maxVerticalUp = Mathf.CeilToInt(maxHeight / blockSize);

        maxHeight -= margin;

        maxJumpDistance = Mathf.FloorToInt(maxDistance / blockSize);
        maxVerticalOffsetUp = Mathf.CeilToInt(maxHeight / blockSize);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    protected override void Start()
    {
        base.Start();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();

        if (hasTriedJump)
        {
            jumpTimer -= Time.deltaTime;
            if (jumpTimer <= 0)
            {
                float deltaX = Mathf.Abs(transform.position.x - beforeJumpX);
                if (deltaX < 0.1f)
                {
                    if (!platformSearching)
                    {
                        horizontal *= -1f;
                    }
                    else
                    {
                        offsetPoint = true;
                    }
                }
                hasTriedJump = false;
            }
        }

        if (stepCooldownTimer > 0)
        {
            stepCooldownTimer -= Time.deltaTime;
        }

        if (onGround && isStepping)
        {
            float newY = Mathf.MoveTowards(transform.position.y, stepTargetY, stepSpeed * Time.deltaTime);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            /*if (Mathf.Approximately(newY, stepTargetY))
            {
                isStepping = false;
            }*/

            isStepping = false;
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (horizontal != 0f)
        {
            horizDup = horizontal > 0 ? 1f : -1f;
        }

        if (horizDup != 0)
        {
            spriteRend.flipX = horizDup < 0;

            if (onGround)
            {
                DetectJumpOpportunity();
            }
        }

    }

    protected override void Vision()
    {
        Vector2 baseDirection;

        if (isTrackingPlayer && targetPlayer != null)
        {
            baseDirection = (targetPlayer.position - transform.position).normalized;
        }
        else
        {
            baseDirection = new Vector2(horizDup, 0).normalized;
        }

        int half = rayCount / 2;

        for (int i = -half; i <= half; i++)
        {
            float angleStep = visionAngle / rayCount;
            float angle = angleStep * i;

            Vector2 rayDirection = Quaternion.Euler(0, 0, angle) * baseDirection;

            RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, rayDirection, visionDist);

            Debug.DrawRay(transform.position, rayDirection * visionDist, Color.yellow);

            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Ground")) break;

                if (hit.collider.CompareTag("Player"))
                {
                    canSeePlayer = true;
                    currentState = EnemyState.Chasing;
                    nextDirectionChangeTime = float.MaxValue;
                    targetPlayer = hit.collider.transform;
                    lastSeenPos = targetPlayer.position;
                    timeSinceSeen = 0f;
                    isLookingAround = false;
                    break;
                }
            }
            canSeePlayer = false;
        }
    }

    protected override void PatrollingState()
    {
        if (Time.time >= nextDirectionChangeTime)
        {
            ChooseDirection();
            nextDirectionChangeTime = Time.time + Random.Range(3f, 30f);
        }

        if (onGround)
        {
            WhatGroundAhead();
        }
        else
        {
            rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        }

    }

    protected override void ChasingState()
    {
        if (targetPlayer == null)
        {
            isTrackingPlayer = false;
            currentState = EnemyState.Patrolling;
            nextDirectionChangeTime = Time.time + Random.Range(3f, 30f);
            return;
        }

        isTrackingPlayer = true;

        MovingRules(targetPlayer.position);

        timeSinceSeen += Time.deltaTime;

        if (timeSinceSeen > lostSightCooldown)
        {
            isTrackingPlayer = false;
            currentState = EnemyState.Searching;
            isLookingAround = false;
            ResetPlatformState();
        }
    }

    protected override void SearchingState()
    {
        float dist = Vector2.Distance(transform.position, lastSeenPos);

        if (!hasStartedSearching)
        {
            searchingTime = Time.time;
            hasStartedSearching = true;
        }

        if (Time.time - searchingTime > 10f)
        {
            currentState = EnemyState.Patrolling;
            nextDirectionChangeTime = Time.time + Random.Range(3f, 30f);
            hasStartedSearching = false;
            ResetPlatformState();
            ChooseDirection();
            return;
        }

        if (!isLookingAround)
        {
            MovingRules(lastSeenPos);
            if (dist < 0.3f)
            {
                isLookingAround = true;
                currentLookTimer = lookAroundTimer;
                horizontal = 0;
            }
        }
        else
        {
            currentLookTimer -= Time.deltaTime;
            rb.velocity = new Vector2(0, rb.velocity.y);
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
                nextDirectionChangeTime = Time.time + Random.Range(3f, 30f);
                hasStartedSearching = false;
                isLookingAround = false;
                ResetPlatformState();
                ChooseDirection();
            }
        }
    }

    protected override void MoveTowards(Vector2 target)
    {
        float directionToTarget = Mathf.Sign(target.x - transform.position.x);
        horizontal = directionToTarget;
    }

    protected override void ChooseDirection()
    {
        int rand = Random.Range(0, 3);
        horizontal = rand == 0 ? -1f : (rand == 1 ? 0f : 1f);
    }

    private void Jump(float jumpFrc)
    {
        if (!hasTriedJump)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpFrc);
            beforeJumpX = transform.position.x;
            hasTriedJump = true;
            jumpTimer = jumpDelay;
        }
    }

    private void WhatGroundAhead()
    {
        switch (EvaluateGroundAhead())
        {
            case GroundResult.GroundClose:
                rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
                break;

            case GroundResult.GroundFar:
                rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
                break;

            case GroundResult.JumpableSameLevel:
                Jump(jumpForce);
                break;

            case GroundResult.JumpableUp:
                Jump(jumpForce);
                break;

            case GroundResult.JumpableDown:
                Jump(jumpForce * 0.8f);
                break;

            case GroundResult.NoGround:
            default:
                if (!isTrackingPlayer || platformSearching)
                {
                    horizontal *= -1;
                }
                break;
        }
    }

    private void MovingRules(Vector2 targetToSeek)
    {
        if (onGround)
        {
            WhatGroundAhead();
        }
        else
        {
            rb.velocity = new Vector2(horizontal * moveSpeed, rb.velocity.y);
        }

        if (isJumpingToPlatform && worldTarget.HasValue)
        {
            HandleJumpingToPlatform(targetToSeek);
            return;
        }
        else if (isDropingFromPlatform && worldTarget.HasValue)
        {
            HandleDroppingFromPlatform(targetToSeek);
        }

        if (!HasStepUpPath(targetToSeek))
        {
            if (targetToSeek.y > transform.position.y)
            {
                HandleUpwardMovement(targetToSeek);
            }
            else if (Mathf.Abs(targetToSeek.x - transform.position.x) < 0.4f && targetToSeek.y < transform.position.y - 0.2f && !canSeePlayer && onGround)
            {
                HandleDownwardMovement(targetToSeek);
            }
            else
            {
                MoveTowards(targetToSeek);
                consecutiveDirJumpAttempts = 0;
            }
        }
        else
        {
            MoveTowards(targetToSeek);
        }
    }

    private void HandleJumpingToPlatform(Vector2 targetToSeek)
    {
        platformSearching = true;
        platformApproachOffset = platformDirection > 0 ? 0.33f : -0.33f;

        targetPos = new Vector2(worldTarget.Value.x + platformApproachOffset, worldTarget.Value.y);
        targetPos2 = new Vector2(worldTarget.Value.x, worldTarget.Value.y);

        float currentDist = Mathf.Abs(transform.position.x - targetPos2.x);

        if (!offsetPoint && currentDist > prevDirToTarg2 + 0.08f)
        {
            offsetPoint = true;
        }

        if (Time.time - lastCheck > 1)
        {
            prevDirToTarg2 = currentDist;
            lastCheck = Time.time;
        }

        if (offsetPoint)
        {
            MoveTowards(targetPos);
        }

        if (Mathf.Abs(transform.position.x - targetPos.x) < 0.02f)
        {
            MoveTowards(targetPos2);
            prevDirToTarg2 = currentDist;
            offsetPoint = false;
        }

        if (onGround && Mathf.Abs(transform.position.x - targetPos2.x) < 0.22f && !offsetPoint)
        {
            if (Time.time - lastJumpAttemptTime > jumpAttemptTimeout)
            {
                Jump(jumpForce);
                lastJumpAttemptTime = Time.time;
                consecutiveJumpAttempts++;
            }
        }

        if (transform.position.y > worldTarget.Value.y && onGround ||
            targetToSeek.y <= transform.position.y ||
            consecutiveJumpAttempts >= 3 ||
            (Mathf.Abs(transform.position.y - worldTarget.Value.y) > maxVerticalUp * blockSize && onGround))
        {
            ResetPlatformJumpState();
        }
    }
    private void HandleDroppingFromPlatform(Vector2 targetToSeek)
    {
        targetPos = new Vector2(worldTarget.Value.x, worldTarget.Value.y);
        MoveTowards(targetPos);

        if (transform.position.y < worldTarget.Value.y || targetToSeek.y >= transform.position.y)
        {
            isDropingFromPlatform = false;
            worldTarget = null;
        }
    }

    private void HandleUpwardMovement(Vector2 targetToSeek)
    {
        if (Time.time - lastTurn > 0.5f)
        {
            MoveTowards(targetToSeek);
            lastTurn = Time.time;
        }

        bool canTryDirectJump = Time.time - lastDirectJumpTime > tryDirectJumpCooldown && consecutiveDirJumpAttempts < 3 && Mathf.Abs(transform.position.y - targetToSeek.y) < maxVerticalUp * blockSize;

        if (canTryDirectJump)
        {
            platformSearching = false;
            if (onGround && Mathf.Abs(transform.position.x - targetToSeek.x) < 0.4f)
            {
                Jump(jumpForce);
                consecutiveDirJumpAttempts++;
                lastDirectJumpTime = Time.time;
                return;
            }
        }
        else if ((consecutiveDirJumpAttempts >= 3 && onGround) || (Mathf.Abs(transform.position.y - targetToSeek.y) >= maxVerticalUp * blockSize && Mathf.Abs(transform.position.x - targetToSeek.x) <= 1 && onGround))
        {
            Vector2Int? jumpTarget = FindPlatform(targetToSeek);

            if (jumpTarget.HasValue)
            {
                worldTarget = GetWorldFromGrid(jumpTarget.Value);
                isJumpingToPlatform = true;
            }

            consecutiveDirJumpAttempts = 0;
        }
    }

    private void HandleDownwardMovement(Vector2 targetToSeek)
    {
        consecutiveDirJumpAttempts = 0;
        Vector2Int? edgeTarget = FindDropEdge(targetToSeek);

        if (edgeTarget.HasValue)
        {
            worldTarget = GetWorldFromGrid(edgeTarget.Value);
            isDropingFromPlatform = true;
        }
    }

    private void DetectJumpOpportunity()
    {
        Vector2 direction = new Vector2(horizontal, 0);
        float checkDistance = 0.12f;
        float checkDistVert = 0.16f;
        float forwardOffset = 0.12f;

        Vector2 bottomPos = new Vector2(transform.position.x, transform.position.y - blockSize);
        Vector2 middlePos = new Vector2(transform.position.x, transform.position.y);
        Vector2 topPos = new Vector2(transform.position.x, transform.position.y + blockSize);
        Vector2 vertOrigin = new Vector2(transform.position.x + (horizontal > 0 ? forwardOffset : -forwardOffset), transform.position.y + 0.16f);

        RaycastHit2D lowHit = Physics2D.Raycast(bottomPos, direction, checkDistance, groundLayer);
        RaycastHit2D midHit = Physics2D.Raycast(middlePos, direction, checkDistance, groundLayer);
        RaycastHit2D topHit = Physics2D.Raycast(topPos, direction, checkDistance, groundLayer);
        RaycastHit2D vertHit = Physics2D.Raycast(vertOrigin, Vector2.down, checkDistVert, groundLayer);

        Debug.DrawRay(bottomPos, direction * checkDistance, Color.red);
        Debug.DrawRay(middlePos, direction * checkDistance, Color.green);
        Debug.DrawRay(topPos, direction * checkDistance, Color.blue);
        Debug.DrawRay(vertOrigin, Vector2.down * checkDistVert, Color.magenta);

        if (lowHit.collider != null && vertHit.collider == null && stepCooldownTimer <= 0)
        {
            stepTargetY = transform.position.y + 0.3f;
            isStepping = true;
            stepCooldownTimer = stepCooldown;
        }
        else if (midHit.collider != null && topHit.collider == null)
        {
            Jump(jumpForce);
        }
        else if (topHit.collider != null)
        {
            Jump(jumpForce);
        }
    }

    private GroundResult EvaluateGroundAhead()
    {
        int currentX, currentY;

        currentX = Mathf.RoundToInt((gridWorldSize.x / 2) / nodeDiameter);
        currentY = Mathf.RoundToInt((gridWorldSize.y / 2) / nodeDiameter);

        int direction = horizontal > 0 ? 1 : -1;
        int forwardX = horizontal < 0 ? currentX + direction : currentX;
        int maxFallDepth = 6;

        List<GroundResult> options = new List<GroundResult>();

        for (int i = 1; i <= maxFallDepth; i++)
        {
            int checkY = currentY - i;

            if (IsInBounds(forwardX, checkY))
            {
                Vector3 point = grid[forwardX, checkY];
                bool isGround = Physics2D.OverlapCircle(point, nodeRadius * 0.1f, groundLayer);
                if (isGround)
                    return i <= 3 ? GroundResult.GroundClose : GroundResult.GroundFar;
            }
        }

        for (int dx = 1; dx <= maxJumpDistance; dx++)
        {
            int targetX = currentX + dx * direction;

            for (int dy = -maxVerticalOffsetDown; dy <= maxVerticalOffsetUp; dy++)
            {
                int targetY = currentY + dy;
                if (!IsInBounds(targetX, targetY)) continue;

                Vector3 targetPos = grid[targetX, targetY];
                bool isGround = Physics2D.OverlapCircle(targetPos, nodeRadius * 0.1f, groundLayer);
                if (!isGround) continue;

                bool spaceAbove = true;
                for (int i = 1; i <= 4; i++)
                {
                    int checkY = targetY + i;
                    if (!IsInBounds(targetX, checkY)) break;

                    Vector3 above = grid[targetX, checkY];
                    if (Physics2D.OverlapCircle(above, nodeRadius * 0.1f, groundLayer))
                    {
                        spaceAbove = false;
                        break;
                    }
                }

                if (!spaceAbove) continue;

                if (dy == 0) options.Add(GroundResult.JumpableSameLevel);
                if (dy > 0) options.Add(GroundResult.JumpableUp);
                if (dy < 0) options.Add(GroundResult.JumpableDown);
            }
        }

        if (options.Count > 0)
            return options[Random.Range(0, options.Count)];

        return GroundResult.NoGround;
    }

    bool HasStepUpPath(Vector2 target)
    {
        Vector2Int current = GetGridFromWorld(transform.position);
        Vector2Int targetGrid = GetGridFromWorld(target);

        int dir = targetGrid.x > current.x ? 1 : -1;
        int maxHorizontalCheck = 6;
        int maxStepHeight = maxVerticalUp - 3;
        int maxStepWidth = 2;

        for (int i = 1; i <= maxHorizontalCheck; i++)
        {
            int checkX = current.x + i * dir;

            for (int j = 1; j <= maxStepHeight; j++)
            {
                int checkY = current.y + j;

                if (!IsInBounds(checkX, checkY)) continue;

                Vector3 stepPoint = grid[checkX, checkY];
                Vector3 belowPoint = grid[checkX, checkY - 1];

                bool spaceFree = !Physics2D.OverlapCircle(stepPoint, nodeRadius * 0.1f, groundLayer);
                bool hasGround = Physics2D.OverlapCircle(belowPoint, nodeRadius * 0.1f, groundLayer);

                bool reachable = Mathf.Abs(checkX - current.x) <= maxStepWidth && j <= maxStepHeight;

                if (spaceFree && hasGround && reachable)
                {
                    return true;
                }
            }
        }

        return false;
    }

    Vector2Int? FindPlatform(Vector2 playerPos)
    {
        Vector2Int current = GetGridFromWorld(transform.position);
        Vector2Int player = GetGridFromWorld(playerPos);

        float bestScore = float.MaxValue;
        float maxDistance = 10f;
        Vector2Int? best = null;

        for (int x = 1; x < gridSizeX - 1; x++)
        {
            for (int y = 1; y < gridSizeY - 4; y++)
            {
                if (!IsInBounds(x, y)) continue;

                Vector3 point = grid[x, y];

                if (!Physics2D.OverlapCircle(point, nodeRadius * 0.1f, groundLayer)) continue;

                bool leftSide = !Physics2D.OverlapCircle(grid[x - 1, y], nodeRadius * 0.1f, groundLayer);
                bool rightSide = !Physics2D.OverlapCircle(grid[x + 1, y], nodeRadius * 0.1f, groundLayer);

                int offsetX = leftSide ? -1 : 1;

                bool spaceAbove = true;
                for (int i = 1; i <= 3; i++)
                {
                    int checkY = y + i;
                    if (!IsInBounds(x, checkY))
                    {
                        spaceAbove = false;
                        break;
                    }

                    Vector3 above = grid[x, checkY];
                    if (Physics2D.OverlapCircle(above, nodeRadius * 0.1f, groundLayer))
                    {
                        spaceAbove = false;
                        break;
                    }
                }
                if (!spaceAbove) continue;

                if (!IsSpaceNear(x, y, offsetX)) continue;

                if (!leftSide && !rightSide) continue;

                int dy = y - current.y;
                if (dy <= 0 || dy > maxVerticalUp - 3) continue;

                Vector2Int target = new Vector2Int(x, y);

                if (!IsInBounds(target.x, target.y)) continue;

                float distToPlayer = Vector2Int.Distance(target, player);
                float distFromCurrent = Vector2Int.Distance(target, current);

                if (distFromCurrent > maxDistance) continue;

                float score = distToPlayer + distFromCurrent;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = target;
                    platformDirection = leftSide ? -1 : 1;
                }
            }

        }
        return best;
    }

    Vector2Int? FindDropEdge(Vector2 playerPos)
    {
        Vector2Int current = GetGridFromWorld(transform.position);
        Vector2Int player = GetGridFromWorld(playerPos);

        float bestScore = float.MaxValue;
        float maxDistance = 10f;
        Vector2Int? best = null;

        for (int x = 1; x < gridSizeX - 1; x++)
        {
            for (int y = 1; y < gridSizeY - 4; y++)
            {
                if (!IsInBounds(x, y)) continue;

                Vector3 point = grid[x, y];

                if (!Physics2D.OverlapCircle(point, nodeRadius * 0.1f, groundLayer)) continue;

                bool leftSide = !Physics2D.OverlapCircle(grid[x - 1, y], nodeRadius * 0.1f, groundLayer);
                bool rightSide = !Physics2D.OverlapCircle(grid[x + 1, y], nodeRadius * 0.1f, groundLayer);

                if (!leftSide && !rightSide) continue;

                int offsetX = leftSide ? -1 : 1;

                bool canFall = false;
                for (int dx = 1; dx <= 2; dx++)
                {
                    for (int dy = 1; dy <= 3; dy++)
                    {
                        int checkX = x + dx * offsetX;
                        int checkY = y - dy;

                        if (!IsInBounds(checkX, checkY)) break;

                        Vector3 below = grid[checkX, checkY];
                        if (!Physics2D.OverlapCircle(below, nodeRadius * 0.1f, groundLayer))
                        {
                            canFall = true;
                            break;
                        }
                    }
                }

                if (!canFall) continue;

                if (y > current.y && player.y < current.y) continue;

                float distToPlayer = Vector2Int.Distance(new Vector2Int(x, y), player);
                float distFromCurrent = Vector2Int.Distance(new Vector2Int(x, y), current);

                if (distFromCurrent > maxDistance) continue;

                float score = distToPlayer + distFromCurrent * 0.5f;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = new Vector2Int(x + 2 * offsetX, y);
                }
            }
        }
        return best;
    }

    bool IsSpaceNear(int targetX, int targetY, int dir, int with = 2, int height = 3)
    {
        for (int dx = 1; dx <= with; dx++)
        {
            for (int dy = 0; dy <= height; dy++)
            {
                int checkX = targetX + dx * dir;
                int checkY = targetY + dy;

                if (!IsInBounds(checkX, checkY)) return false;

                Vector3 pos = grid[checkX, checkY];
                if (Physics2D.OverlapCircle(pos, nodeRadius * 0.1f, groundLayer))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void ResetPlatformState()
    {
        offsetPoint = true;
        isJumpingToPlatform = false;
        worldTarget = null;
        consecutiveJumpAttempts = 0;
        platformApproachOffset = 0;
        platformSearching = false;
    }

    private void ResetPlatformJumpState()
    {
        offsetPoint = true;
        isJumpingToPlatform = false;
        worldTarget = null;
        consecutiveJumpAttempts = 0;
        platformApproachOffset = 0;
    }

    private void OnDrawGizmos()
    {
        if (worldTarget.HasValue)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(worldTarget.Value, nodeRadius);
        }
    }
}
