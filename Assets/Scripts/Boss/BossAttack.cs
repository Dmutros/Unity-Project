using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossAttack
{
    public virtual void FaceTarget(GameObject boss, Transform player)
    {
        if (player == null) return;

        Vector3 scale = boss.transform.localScale;
        if (player.position.x > boss.transform.position.x && boss.transform.localScale.x > 0)
            scale.x = -Mathf.Abs(scale.x);
        else if (player.position.x < boss.transform.position.x && boss.transform.localScale.x < 0)
            scale.x = Mathf.Abs(scale.x);

        boss.transform.localScale = scale;
    }
    public abstract IEnumerator Execute(GameObject boss);

    public virtual void StopAttack() { }
}

public class DashBiteAttack : BossAttack
{
    public float dashDistance = 0.32f;
    public float dashSpeed = 5f;
    public float dashDamageMultiplier = 2f;
    public float dashKnockbackMultiplier = 2f;

    public override IEnumerator Execute(GameObject boss)
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player == null) yield break;

        FaceTarget(boss, player);

        ContactDamage contact = boss.GetComponent<ContactDamage>();

        if (contact != null)
        {
            contact.damageAmount *= dashDamageMultiplier;
            contact.knockbackForce *= dashKnockbackMultiplier;
        }

        Vector2 direction = (player.position - boss.transform.position).normalized;
        direction.y = 0f;



        Animator animator = boss.GetComponent<Animator>();
        animator.SetTrigger("Dash");

        yield return new WaitForSeconds(0.8f);

        yield return new WaitForSeconds(0.3f);

        float distanceMoved = 0f;

        while (distanceMoved < dashDistance)
        {
            float move = dashSpeed * Time.deltaTime;
            boss.transform.position += (Vector3)direction * move;
            distanceMoved += move;
            yield return null;
        }

        if (contact != null)
        {
            contact.damageAmount /= dashDamageMultiplier;
            contact.knockbackForce /= dashKnockbackMultiplier;
        }

        yield return new WaitForSeconds(0.2f);
    }
}

public class JumpSlamAttack : BossAttack
{
    public float jumpHeight = 1f;
    public float jumpDuration = 0.8f;
    public GameObject wavePrefab;

    public override IEnumerator Execute(GameObject boss)
    {
        if (!IsGrounded(boss)) yield break;

        Vector3 startPos = boss.transform.position;
        float elapsed = 0f;

        Animator animator = boss.GetComponent<Animator>();
        animator.SetTrigger("Jump");
        animator.SetBool("onGround", false);
        yield return new WaitForSeconds(0.4f);

        while (elapsed < jumpDuration)
        {
            float t = elapsed / jumpDuration;
            float height = Mathf.Sin(Mathf.PI * t) * jumpHeight;
            boss.transform.position = startPos + Vector3.up * height;
            elapsed += Time.deltaTime;
            yield return null;
        }

        while (!IsGrounded(boss))
        {
            yield return null;
        }
        animator.SetBool("onGround", true);

        Vector3 spawnPos = boss.transform.position;
        RaycastHit2D hit = Physics2D.Raycast(spawnPos, Vector2.down, 2f, LayerMask.GetMask("Ground"));
        if (hit.collider != null)
        {
            spawnPos.y = hit.point.y + 0.05f;
        }

        if (wavePrefab != null)
        {
            GameObject left = GameObject.Instantiate(wavePrefab, spawnPos, Quaternion.identity);
            GameObject right = GameObject.Instantiate(wavePrefab, spawnPos, Quaternion.identity);

            left.GetComponent<WaveProjectile>()?.SetDirection(Vector2.left);
            right.GetComponent<WaveProjectile>()?.SetDirection(Vector2.right);
        }

        yield return new WaitForSeconds(0.4f);
    }

    bool IsGrounded(GameObject boss)
    {
        Vector2 position = boss.transform.position;
        Vector2 down = Vector2.down;
        float distance = 0.2f;
        LayerMask groundLayer = LayerMask.GetMask("Ground");

        RaycastHit2D hit = Physics2D.Raycast(position, down, distance, groundLayer);
        return hit.collider != null;
    }
}

public class SpitAttack : BossAttack
{
    public GameObject spitPrefab;
    public float spitForce = 8f;
    public Transform spitPoint;
    public float maxSpitAngle = 60f;
    public override IEnumerator Execute(GameObject boss)
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null || spitPrefab == null || spitPoint == null) yield break;

        FaceTarget(boss, player);

        Animator animator = boss.GetComponent<Animator>();
        animator.SetTrigger("Spit");
        yield return new WaitForSeconds(0.8f);

        Vector2 toPlayer = (player.position - spitPoint.position).normalized;
        Vector2 forward = boss.transform.localScale.x > 0 ? Vector2.left : Vector2.right;

        float angle = Vector2.Angle(forward, toPlayer);
        if (angle > maxSpitAngle)
        {
            yield break;
        }

        GameObject spit = GameObject.Instantiate(spitPrefab, spitPoint.position, Quaternion.identity);
        spit.GetComponent<SpitProjectile>()?.SetDirection(toPlayer);

        yield return new WaitForSeconds(0.5f);
    }
}

public class SpitBurstAttack : BossAttack
{
    public GameObject bigSpitPrefab;
    public Transform spitPoint;
    public float delayBeforeExplode = 1.5f;

    public int numberOfProjectiles = 3;
    public float timeBetweenSpits = 0.3f;
    public float spreadAngle = 15f;

    public override IEnumerator Execute(GameObject boss)
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) yield break;

        FaceTarget(boss, player);

        Animator animator = boss.GetComponent<Animator>();
        animator.SetTrigger("Spit");

        yield return new WaitForSeconds(0.6f);

        for (int i = 0; i < numberOfProjectiles; i++)
        {
            GameObject projectile = GameObject.Instantiate(bigSpitPrefab, spitPoint.position, Quaternion.identity);
            SpitBurstProjectile burst = projectile.GetComponent<SpitBurstProjectile>();

            if (burst != null && player != null)
            {
                burst.SetOwner(boss);

                Vector2 toPlayer = (player.position - spitPoint.position).normalized;

                Vector2 direction = toPlayer;

                if (i > 0)
                {
                    float angleOffset = Random.Range(-spreadAngle, spreadAngle);
                    direction = Quaternion.Euler(0, 0, angleOffset) * toPlayer;
                }

                burst.SetDirection(direction.normalized);
                burst.StartFuse(delayBeforeExplode);
            }

            yield return new WaitForSeconds(timeBetweenSpits);
        }

        yield return new WaitForSeconds(0.5f);
    }
}

public class InsectLineAttack : BossAttack
{
    public GameObject lineWarningPrefab;
    public GameObject insectProjectilePrefab;
    public float gridWidth = 14f;
    public float gridHeight = 12f;
    public float lineSpacing = 1.2f;
    public float warningDuration = 0.8f;
    public float projectileSpeed = 7f;
    public float lineLength = 200f;

    public int burstCount = 3;
    public float burstDelay = 0.5f;

    private List<GameObject> warnings = new List<GameObject>();

    public enum GridPattern
    {
        Full,
        VerticalOnly,
        HorizontalOnly,
        FullDiagonal,
        DiagonalOnly1,
        DiagonalOnly2
    }
    private bool attackActive;
    public override IEnumerator Execute(GameObject boss)
    {
        attackActive = true;
        try
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj == null) yield break;
            Transform player = playerObj.transform;

            FaceTarget(boss, player);

            for (int i = 0; i < burstCount; i++)
            {
                GridPattern pattern = GetRandomPattern();
                yield return ExecuteSingleBurst(player.position, pattern);
                yield return new WaitForSeconds(burstDelay);
            }

            yield return new WaitForSeconds(0.4f);
        }
        finally
        {
            if (attackActive)
                boss.GetComponent<FlyingMovementBehavior>()?.SetAttackInProgress(false);
        }

        yield return new WaitForSeconds(0.4f);
    }

    private GridPattern GetRandomPattern()
    {
        float roll = Random.value;
        if (roll < 0.15f) return GridPattern.VerticalOnly;
        if (roll < 0.30f) return GridPattern.HorizontalOnly;
        if (roll < 0.45f) return GridPattern.DiagonalOnly1;
        if (roll < 0.60f) return GridPattern.DiagonalOnly2;
        if (roll < 0.80f) return GridPattern.FullDiagonal;
        return GridPattern.Full;
    }

    private IEnumerator ExecuteSingleBurst(Vector2 center, GridPattern pattern)
    {
        var firePoints = new List<(Vector2 pos, Vector2 dir)>();

        switch (pattern)
        {
            case GridPattern.VerticalOnly:
                for (float x = center.x - gridWidth / 2; x <= center.x + gridWidth / 2; x += lineSpacing)
                {
                    firePoints.Add((new Vector2(x, center.y - gridHeight / 2), Vector2.up));
                    firePoints.Add((new Vector2(x, center.y + gridHeight / 2), Vector2.down));
                }
                break;

            case GridPattern.HorizontalOnly:
                for (float y = center.y - gridHeight / 2; y <= center.y + gridHeight / 2; y += lineSpacing)
                {
                    firePoints.Add((new Vector2(center.x - gridWidth / 2, y), Vector2.right));
                    firePoints.Add((new Vector2(center.x + gridWidth / 2, y), Vector2.left));
                }
                break;

            case GridPattern.DiagonalOnly1:
                {
                    Vector2 dir = (Vector2.up + Vector2.right).normalized;
                    Vector2 ortho = new Vector2(-dir.y, dir.x);
                    int lines = Mathf.CeilToInt(gridWidth / lineSpacing);
                    for (int i = -lines / 2; i <= lines / 2; i++)
                    {
                        Vector2 offset = ortho * (i * lineSpacing);
                        Vector2 start = center + offset - dir * (gridHeight / 2);
                        firePoints.Add((start, dir));
                    }
                    break;
                }

            case GridPattern.DiagonalOnly2:
                {
                    Vector2 dir = (Vector2.up + Vector2.left).normalized;
                    Vector2 ortho = new Vector2(-dir.y, dir.x);
                    int lines = Mathf.CeilToInt(gridWidth / lineSpacing);
                    for (int i = -lines / 2; i <= lines / 2; i++)
                    {
                        Vector2 offset = ortho * (i * lineSpacing);
                        Vector2 start = center + offset - dir * (gridHeight / 2);
                        firePoints.Add((start, dir));
                    }
                    break;
                }

            case GridPattern.FullDiagonal:
                for (float offset = -gridWidth / 2; offset <= gridWidth / 2; offset += lineSpacing)
                {
                    firePoints.Add((center + new Vector2(offset, -gridHeight / 2), Vector2.up + Vector2.right));
                    firePoints.Add((center + new Vector2(offset, gridHeight / 2), Vector2.down + Vector2.right));
                    firePoints.Add((center + new Vector2(offset, -gridHeight / 2), Vector2.up + Vector2.left));
                    firePoints.Add((center + new Vector2(offset, gridHeight / 2), Vector2.down + Vector2.left));
                }
                break;

            case GridPattern.Full:
            default:
                for (float x = center.x - gridWidth / 2; x <= center.x + gridWidth / 2; x += lineSpacing)
                {
                    firePoints.Add((new Vector2(x, center.y - gridHeight / 2), Vector2.up));
                    firePoints.Add((new Vector2(x, center.y + gridHeight / 2), Vector2.down));
                }
                for (float y = center.y - gridHeight / 2; y <= center.y + gridHeight / 2; y += lineSpacing)
                {
                    firePoints.Add((new Vector2(center.x - gridWidth / 2, y), Vector2.right));
                    firePoints.Add((new Vector2(center.x + gridWidth / 2, y), Vector2.left));
                }
                for (float offset = -gridWidth / 2; offset <= gridWidth / 2; offset += lineSpacing)
                {
                    firePoints.Add((center + new Vector2(offset, -gridHeight / 2), Vector2.up + Vector2.right));
                    firePoints.Add((center + new Vector2(offset, gridHeight / 2), Vector2.down + Vector2.right));
                    firePoints.Add((center + new Vector2(offset, -gridHeight / 2), Vector2.up + Vector2.left));
                    firePoints.Add((center + new Vector2(offset, gridHeight / 2), Vector2.down + Vector2.left));
                }
                break;
        }

        try
        {
            foreach (var (pos, dir) in firePoints)
            {
                var line = GameObject.Instantiate(lineWarningPrefab, pos, Quaternion.identity);
                line.transform.up = dir.normalized;
                line.transform.localScale = new Vector3(0.2f, lineLength, 1f);
                var sr = line.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sortingOrder = 100;
                warnings.Add(line);
            }

            yield return new WaitForSeconds(warningDuration);

            foreach (var (pos, dir) in firePoints)
            {
                var bug = GameObject.Instantiate(insectProjectilePrefab, pos, Quaternion.identity);
                var insect = bug.GetComponent<InsectProjectile>();
                if (insect != null)
                {
                    insect.Initialize(dir);
                }
            }
        }
        finally
        {
            foreach (var line in warnings)
                if (line != null) GameObject.Destroy(line);
            warnings.Clear();
        }
    }
    public override void StopAttack()
    {
        attackActive = false;
        foreach (var line in warnings)
        {
            if (line != null)
            {
                GameObject.Destroy(line);
            }
        }
        warnings.Clear();
    }
}

public class BombDropAttack : BossAttack
{
    public GameObject bombPrefab;
    public Transform dropPoint;
    public int bombCount = 5;
    public float interval = 0.6f;
    public float driftSpeed = 1.5f;
    public float driftDuration = 3f;

    public override IEnumerator Execute(GameObject boss)
    {
        if (bombPrefab == null || dropPoint == null) yield break;

        FlyingMovementBehavior movement = boss.GetComponent<FlyingMovementBehavior>();
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (movement == null || player == null) yield break;

        Vector2 direction = (player.position.x > boss.transform.position.x) ? Vector2.right : Vector2.left;
        movement.FaceDirection(direction.x);
        movement.StartHorizontalDrift(driftSpeed, driftDuration, direction);

        for (int i = 0; i < bombCount; i++)
        {
            GameObject bomb = GameObject.Instantiate(bombPrefab, dropPoint.position, Quaternion.identity);
            Rigidbody2D rb = bomb.GetComponent<Rigidbody2D>();

            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }

            yield return new WaitForSeconds(interval);
        }

        yield return new WaitForSeconds(0.5f);
    }
}
