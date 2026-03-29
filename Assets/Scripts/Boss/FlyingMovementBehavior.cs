using System.Collections;
using UnityEngine;

public class FlyingMovementBehavior : MonoBehaviour
{
    public float hoverHeight;
    public float horizontalOffsetRange = 2f;
    public float moveSpeed = 2f;
    public float takeoffSpeed = 3f;
    public float landingSpeed = 2f;
    public float groundYOffset = 0.1f;

    private Transform target;
    private Vector2 targetPosition;

    private Rigidbody2D rb;
    private float originalGravity;

    private enum State { Idle, TakingOff, Hovering, Landing }
    private State currentState = State.Idle;

    private Vector2 groundPosition;

    private Animator animator;
    private int groundLayerIndex;
    private int airLayerIndex;

    private bool horizontalPaused = false;

    public void PauseHorizontalMovement() => horizontalPaused = true;
    public void ResumeHorizontalMovement() => horizontalPaused = false;

    private Coroutine horizontalMoveRoutine;
    private Collider2D col;

    public float hoverZoneWidth = 6f;
    public float hoverZoneHeight = 2f;
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;

        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player")?.transform;

        col = GetComponent<Collider2D>();
    }

    public void StartBehavior()
    {
        enabled = true;
        if (rb != null) rb.gravityScale = 0f;
        currentState = State.TakingOff;
        IsLanded = false;
        SetGhostMode(true);
        groundPosition = transform.position;
        PickNewHoverPosition();

        if (landingRoutine != null)
        {
            StopCoroutine(landingRoutine);
            landingRoutine = null;
        }
    }

    private Coroutine landingRoutine;

    public void StopBehavior()
    {
        currentState = State.Landing;
        IsLanded = false;
        if (landingRoutine == null)
            landingRoutine = StartCoroutine(LandingRoutine());
    }

    private void Update()
    {
        if (!IsLanded && animator.GetBool("isFlying") == false)
        {
            Debug.LogWarning("Animator desync! Forcing isFlying true");
            animator.SetBool("isFlying", true);
        }
        else if (IsLanded && animator.GetBool("isFlying") == true)
        {
            Debug.LogWarning("Animator desync! Forcing isFlying true");
            animator.SetBool("isFlying", false);
        }

    }
    public void UpdateBehavior()
    {
        if (isAttackInProgress) return;

        switch (currentState)
        {
            case State.TakingOff:
                PerformTakeoff();
                break;
            case State.Hovering:
                HoverMovement();
                break;
                /*case State.Landing:
                    PerformLanding();
                    break;*/
        }
    }


    private void PerformTakeoff()
    {
        animator.SetBool("isFlying", true);

        Vector2 targetTakeoffPos = new Vector2(transform.position.x, target.position.y + hoverHeight);
        transform.position = Vector2.MoveTowards(transform.position, targetTakeoffPos, takeoffSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, targetTakeoffPos) < 0.1f)
        {
            currentState = State.Hovering;
            PickNewHoverPosition();
        }
    }

    private void HoverMovement()
    {
        if (target == null) return;

        Vector2 zoneCenter = new Vector2(target.position.x, target.position.y + hoverHeight);
        Vector2 zoneMin = zoneCenter - new Vector2(hoverZoneWidth / 2f, hoverZoneHeight / 2f);
        Vector2 zoneMax = zoneCenter + new Vector2(hoverZoneWidth / 2f, hoverZoneHeight / 2f);

        Vector2 currentPos = rb.position;

        bool insideZone = currentPos.x >= zoneMin.x && currentPos.x <= zoneMax.x && currentPos.y >= zoneMin.y && currentPos.y <= zoneMax.y;

        Vector2 targetPos;
        if (insideZone)
        {
            float xOffset = Mathf.Sin(Time.time * 2f) * (hoverZoneWidth / 3f);
            targetPos = new Vector2(target.position.x + xOffset, target.position.y + hoverHeight);
        }
        else
        {
            targetPos = zoneCenter;
        }

        Vector2 newPos = Vector2.MoveTowards(rb.position, targetPos, moveSpeed * Time.deltaTime);
        rb.MovePosition(newPos);

        FaceDirection(targetPos.x - rb.position.x);
    }

    public void FaceDirection(float directionX)
    {
        if (directionX > 0.1f && transform.localScale.x > 0)
        {
            Flip();
        }
        else if (directionX < -0.1f && transform.localScale.x < 0)
        {
            Flip();
        }
    }
    private void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }


    public bool IsLanded { get; private set; } = true;
    private IEnumerator LandingRoutine()
    {
        Debug.Log("LandingRoutine started");
        float waitTime = 5f;
        while (isAttackInProgress && waitTime > 0f)
        {
            waitTime -= Time.deltaTime;
            yield return null;
        }
        if (isAttackInProgress)
        {
            Debug.LogWarning("LandingRoutine: Attack never finished. Forcing continue.");
            SetAttackInProgress(false);
        }


        Vector2 landTarget = FindLandingSpot(searchRadius: 6f, step: 1f, maxRayDistance: 10f, groundMask: LayerMask.GetMask("Ground"));
        Debug.Log("Landing target: " + landTarget);

        int safetyCounter = 0;

        while (Vector2.Distance(transform.position, landTarget) >= 0.5f)
        {
            transform.position = Vector2.MoveTowards(transform.position, landTarget, landingSpeed * Time.deltaTime);
            yield return null;

            safetyCounter++;
            if (safetyCounter > 500)
            {
                SetGhostMode(false);
                Debug.LogWarning("LandingRoutine: stuck in landing loop. Forcing landing.");
                break;
            }
        }
        SetGhostMode(false);

        Debug.Log("Landing complete. Setting grounded state");

        animator.SetBool("isFlying", false);

        if (rb != null) rb.gravityScale = originalGravity;

        IsLanded = true;
        enabled = false;
        currentState = State.Idle;
        landingRoutine = null;
    }


    private void PickNewHoverPosition()
    {
        if (target == null) return;

        float offsetX = Random.Range(-horizontalOffsetRange, horizontalOffsetRange);
        targetPosition = new Vector2(target.position.x + offsetX, target.position.y + hoverHeight);
    }
    public IEnumerator WaitUntilInAir(float threshold = 0.1f)
    {
        while (Mathf.Abs(transform.position.y - (target.position.y + hoverHeight)) > threshold)
        {
            yield return null;
        }
    }

    public void StartHorizontalDrift(float speed, float duration, Vector2 direction)
    {
        if (horizontalMoveRoutine != null)
            StopCoroutine(horizontalMoveRoutine);
        horizontalMoveRoutine = StartCoroutine(HorizontalDriftRoutine(speed, duration, direction));
    }

    private IEnumerator HorizontalDriftRoutine(float speed, float duration, Vector2 direction)
    {
        float timer = 0f;
        while (timer < duration)
        {
            transform.position += (Vector3)(direction.normalized * speed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    public bool IsHovering()
    {
        return currentState == State.Hovering;
    }

    private bool isAttackInProgress = false;
    public void SetAttackInProgress(bool value)
    {
        isAttackInProgress = value;
    }
    public bool IsAttackInProgress()
    {
        return isAttackInProgress;
    }

    public void SetGhostMode(bool enabled)
    {
        if (col != null)
            col.isTrigger = enabled;
    }

    private Vector2 FindLandingSpot(float searchRadius = 5f, float step = 1f, float maxRayDistance = 10f, LayerMask groundMask = default)
    {
        if (target == null) return transform.position;

        Vector2 origin = target.position;
        for (float offset = 0f; offset <= searchRadius; offset += step)
        {
            foreach (float dir in new float[] { 1f, -1f })
            {
                Vector2 checkPos = new Vector2(origin.x + dir * offset, origin.y);

                RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, maxRayDistance, groundMask);
                if (hit.collider != null)
                {
                    Debug.Log("Hit.col " + hit.collider);
                    Debug.Log("Vectr " + new Vector2(checkPos.x, hit.point.y + groundYOffset));
                    return new Vector2(checkPos.x, hit.point.y + groundYOffset);
                }
                
            }
        }
        return new Vector2(transform.position.x, groundPosition.y + groundYOffset);
    }

    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector2 zoneCenter = new Vector2(target.position.x, target.position.y + hoverHeight);
        Vector2 zoneSize = new Vector2(hoverZoneWidth, hoverZoneHeight);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(zoneCenter, zoneSize);
    }


}
