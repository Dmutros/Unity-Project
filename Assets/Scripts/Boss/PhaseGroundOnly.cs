using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhaseGroundOnly : BossPhase
{
    private GroundOnlyBehavior behavior;
    public PhaseGroundOnly(GameObject boss, GameObject wavePrefab, GameObject spitPrefab) : base(boss)
    {
        behavior = new GroundOnlyBehavior(boss, wavePrefab, spitPrefab);
    }

    public override void EnterPhase()
    {
        behavior.StartBehavior();
    }

    public override void UpdatePhase()
    {
        behavior.UpdateBehavior();
    }

    public override void ExitPhase()
    {
        behavior.StopBehavior();
    }
}
