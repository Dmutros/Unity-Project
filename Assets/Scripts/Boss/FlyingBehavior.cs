using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingBehavior : IBossSubBehavior
{
    private GameObject bossObject;
    private FlyingMovementBehavior movement;
    private MonoBehaviour mono;
    private Coroutine attackLoop;

    private List<BossAttack> attacks;
    private int attackCounter = 0;
    private bool isAttacking = false;
    public FlyingBehavior(GameObject boss, GameObject spitBurstPrefab, Transform spitPoint, GameObject bombPrefab, Transform dropPoint, GameObject lineIndicatorPrefab, GameObject insectProjectilePrefab)
    {
        bossObject = boss;
        mono = boss.GetComponent<MonoBehaviour>();

        attacks = new List<BossAttack>()
    {
        new SpitBurstAttack { spitPoint = spitPoint, bigSpitPrefab = spitBurstPrefab },
        new InsectLineAttack { lineWarningPrefab = lineIndicatorPrefab, insectProjectilePrefab = insectProjectilePrefab },
        new BombDropAttack { bombPrefab = bombPrefab, dropPoint = dropPoint },
    };
    }
    public void StartBehavior()
    {
        if (attackLoop != null)
        {
            mono.StopCoroutine(attackLoop);
            attackLoop = null;
        }

        movement = bossObject.GetComponent<FlyingMovementBehavior>();
        movement?.StartBehavior();

        mono.StartCoroutine(StartAttackLoopAfterTakeoff());
    }

    private IEnumerator StartAttackLoopAfterTakeoff()
    {
        yield return mono.StartCoroutine(movement.WaitUntilInAir());

        while (!movement.IsHovering()) yield return null;

        attackLoop = mono.StartCoroutine(AttackLoop());
    }

    public void StopBehavior()
    {
        if (attackLoop != null)
            mono.StopCoroutine(attackLoop);

        foreach (var atk in attacks)
        {
            atk.StopAttack();
            movement?.SetAttackInProgress(false);
        }

        isAttacking = false;
        movement?.StopBehavior();
    }

    public void UpdateBehavior()
    {
        movement?.UpdateBehavior();
    }

    private IEnumerator AttackLoop()
    {
        Debug.Log("[FlyingBehavior] Starting attack loop");
        while (true)
        {
            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null)
            {
                yield return null;
                continue;
            }

            if (isAttacking)
            {
                yield return null;
                continue;
            }

            if (!movement.IsHovering())
            {
                yield return null;
                continue;
            }

            movement?.SetAttackInProgress(true);

            isAttacking = true;
            /*movement?.PauseHorizontalMovement();*/

            BossAttack selectedAttack = attacks[Random.Range(0, attacks.Count)];

            bool attackFinished = false;

            IEnumerator AttackWrapperWithTimeout(IEnumerator exec, float timeout = 10f)
            {
                float timer = 0f;
                while (true)
                {
                    if (timer > timeout)
                    {
                        Debug.LogWarning("[FlyingBehavior] Attack timed out.");
                        break;
                    }

                    object current;
                    try
                    {
                        if (!exec.MoveNext())
                            break;
                        current = exec.Current;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[FlyingBehavior] Attack threw exception: {ex}");
                        break;
                    }

                    timer += Time.deltaTime;
                    yield return current;
                }

                isAttacking = false;
                movement?.SetAttackInProgress(false);
            }

            IEnumerator exec = selectedAttack.Execute(bossObject);
            yield return mono.StartCoroutine(AttackWrapperWithTimeout(exec, 10f));

            movement?.ResumeHorizontalMovement();
            isAttacking = false;
            yield return new WaitForSeconds(1f);
            movement?.SetAttackInProgress(false);

            attackCounter++;
            yield return new WaitForSeconds(attackCounter % 3 == 0 ? 1.2f : 0.5f);
            yield return new WaitForSeconds(Random.Range(1.0f, 2.0f));
        }
    }


}
