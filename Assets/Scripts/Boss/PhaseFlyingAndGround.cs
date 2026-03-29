using System.Collections;
using UnityEngine;

public class PhaseFlyingAndGround : BossPhase
{
    private GroundOnlyBehavior groundBehavior;
    private FlyingBehavior flyingBehavior;

    private MonoBehaviour mono;
    private Coroutine phaseLoop;

    private float groundDuration = 8f;
    private float airDuration = 15f;

    private GameObject bossObject;

    public PhaseFlyingAndGround(GameObject boss, GameObject wavePrefab, GameObject spitPrefab, GameObject spitBurstPrefab, GameObject bombPrefab, GameObject lineIndicatorPrefab, GameObject insectProjectilePrefab) : base(boss)
    {
        bossObject = boss;

        mono = boss.GetComponent<MonoBehaviour>();

        groundBehavior = new GroundOnlyBehavior(boss, wavePrefab, spitPrefab);

        flyingBehavior = new FlyingBehavior(boss, spitBurstPrefab, boss.transform.Find("FlyingSpitPoint"), bombPrefab, boss.transform.Find("DropPoint"), lineIndicatorPrefab, insectProjectilePrefab);

    }

    public override void EnterPhase()
    {
        phaseLoop = mono.StartCoroutine(PhaseRoutine());
    }

    public override void ExitPhase()
    {
        if (phaseLoop != null)
            mono.StopCoroutine(phaseLoop);

        groundBehavior.StopBehavior();
        flyingBehavior.StopBehavior();
    }

    public override void UpdatePhase()
    {
        groundBehavior?.UpdateBehavior();
        flyingBehavior?.UpdateBehavior();
    }

    private IEnumerator PhaseRoutine()
    {
        while (true)
        {

            flyingBehavior.StopBehavior();

            var flyMove = bossObject.GetComponent<FlyingMovementBehavior>();
            while (flyMove != null && !flyMove.IsLanded)
            {
                yield return null;
            }

            groundBehavior.StartBehavior();

            yield return new WaitForSeconds(groundDuration);

            groundBehavior.StopBehavior();

            yield return new WaitForSeconds(1);

            flyingBehavior.StartBehavior();

            yield return new WaitForSeconds(airDuration);

            flyingBehavior.StopBehavior();

        }
    }
}
