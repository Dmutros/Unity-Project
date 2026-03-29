using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class GroundOnlyBehavior : IBossSubBehavior
{
    private GameObject bossObject;
    private GroundMovementBehavior movement;
    private MonoBehaviour mono;
    private List<BossAttack> attacks;
    private Coroutine attackLoop;
    private GameObject wavePrefab;
    private GameObject spitPrefab;
    private Transform spitPoint;

    private bool isAttacking = false;
    private int attackCounter = 0;

    public GroundOnlyBehavior(GameObject boss, GameObject wavePrefab, GameObject spitPrefab)
    {
        this.bossObject = boss;
        this.wavePrefab = wavePrefab;
        this.spitPrefab = spitPrefab;

        mono = boss.GetComponent<MonoBehaviour>();
        spitPoint = boss.transform.Find("SpitPoint");

        attacks = new List<BossAttack>()
        {
            new DashBiteAttack(),
            new JumpSlamAttack {wavePrefab = wavePrefab},
            new SpitAttack {spitPrefab = spitPrefab, spitPoint = spitPoint}
        };
    }

    public void StartBehavior()
    {
        movement = bossObject.GetComponent<GroundMovementBehavior>();
        movement?.StartBehavior();

        attackLoop = mono.StartCoroutine(AttackRoutine());
    }

    public void StopBehavior()
    {
        movement?.StopBehavior();
        if (attackLoop != null)
            mono.StopCoroutine(attackLoop);
    }

    public void UpdateBehavior()
    {
        if (!isAttacking)
            movement?.UpdateBehavior();
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) yield return null;

/*            var flyingMove = bossObject.GetComponent<FlyingMovementBehavior>();
            if (flyingMove != null)
            {
                while (!flyingMove.IsLandingComplete)
                    yield return null;
            }*/

            float distance = Vector2.Distance(player.position, bossObject.transform.position);

            if (Random.value < 0.4f)
            {
                if (distance > 0.8f)
                {
                    movement?.StartBehavior();

                    float timer = Random.Range(1f, 2f);
                    while (timer > 0f)
                    {
                        player = GameObject.FindGameObjectWithTag("Player")?.transform;
                        if (player == null) break;

                        distance = Vector2.Distance(player.position, bossObject.transform.position);

                        if (distance <= 0.8f)
                        {
                            movement?.StopBehavior();
                            break;
                        }

                        timer -= Time.deltaTime;
                        yield return null;
                    }

                    movement?.StopBehavior();
                }
                else
                {
                    yield return new WaitForSeconds(Random.Range(0.5f, 1f));
                    movement?.StopBehavior();
                }

                continue;
            }

            BossAttack attackToUse = null;

            if (distance < 0.8f)
            {
                attackToUse = attacks.Find(a => a is DashBiteAttack);
            }
            else if (distance < 3f)
            {
                bool doJump = Random.value < 0.5f;
                attackToUse = attacks.Find(a => doJump ? a is JumpSlamAttack : a is SpitAttack);
            }
            else
            {
                attackToUse = attacks.Find(a => a is SpitAttack);
            }

            isAttacking = true;
            movement?.StopBehavior();

            yield return mono.StartCoroutine(attackToUse.Execute(bossObject));

            attackCounter++;

            if (attackCounter >= 2)
            {
                yield return new WaitForSeconds(1.5f);
                attackCounter = 0;
            }
            else
            {
                yield return new WaitForSeconds(0.6f);
            }

            movement?.StartBehavior();
            isAttacking = false;
        }
    }
}
