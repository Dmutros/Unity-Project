using UnityEngine;
using UnityEngine.Playables;


public abstract class BossPhase
{
    protected GameObject bossObject;

    public BossPhase(GameObject boss)
    {
        bossObject = boss;
    }

    public abstract void EnterPhase();
    public abstract void UpdatePhase();
    public abstract void ExitPhase();
}
