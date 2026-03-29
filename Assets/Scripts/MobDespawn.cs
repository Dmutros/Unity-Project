using UnityEngine;

public class MobDespawn : MonoBehaviour
{
    public float despawnDistance = 5f;
    private Transform player;

    public bool playerDead = false;

    private void Start()
    {
        player = GameObject.FindWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        if (playerDead)
        {
            bossStp();
        }

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist > despawnDistance)
        {
            bossStp();
            Destroy(gameObject);
        }
    }

    public void bossStp()
    {
        BossPhaseManager bp = GetComponent<BossPhaseManager>();
        if (bp != null)
        {
            FindObjectOfType<MobSpawner>().allowMobSpawning = true;

            BossHealthUI bu = FindObjectOfType<BossHealthUI>();
            if (bu != null)
            {
                bu.healthBarOff(false);
            }
        }
        Destroy(gameObject);
    }
}
