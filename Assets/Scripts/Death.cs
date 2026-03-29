using System.Collections;
using UnityEngine;

public class Death : MonoBehaviour
{
    public GameObject[] gorePrefabs;
    public int goreCount = 6;
    public float spreadAngle = 65f;

    public void Die(Vector2 hitDir, float force)
    {
        Vector2 baseDir = hitDir.normalized;

        foreach (var gorePrefab in gorePrefabs)
        {
            GameObject gore = Instantiate(gorePrefab, transform.position, Quaternion.identity);
            Rigidbody2D rb = gore.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float angleOffset = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);
                Vector2 spreadDir = Quaternion.Euler(0, 0, angleOffset) * baseDir;

                rb.AddForce(spreadDir.normalized * force * 1.5f, ForceMode2D.Impulse);
                rb.AddTorque(Random.Range(-25, 25));
            }
        }

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


        if (gameObject.CompareTag("Player"))
        {
            FindObjectOfType<MobDespawn>().playerDead = true;
            FindObjectOfType<MobSpawner>()?.RespawnPlayer();
        }
        else
        {
            Destroy(gameObject);
        }

    }

}
