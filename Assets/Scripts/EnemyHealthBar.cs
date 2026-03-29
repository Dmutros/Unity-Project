using UnityEngine;

public class EnemyHealthBar : MonoBehaviour
{
    public SpriteRenderer fillBar;
    public SpriteRenderer backgroundBar;
    public SpriteRenderer backgroundLine;
    private HealthData health;
    private bool visibleOnce = false;
    public void SetTarget(HealthData data)
    {
        health = data;
        UpdateFill();
        SetVisible(false);
    }

    private float xScale;

    private void Awake()
    {
        xScale = fillBar.transform.localScale.x;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        if (health == null) return;

        UpdateFill();

        transform.rotation = Quaternion.identity;

        if (!visibleOnce && health.CurrentHealth < health.MaxHealth)
        {
            SetVisible(true);
            visibleOnce = true;
        }

        if (health.IsDead)
        {
            Destroy(gameObject);
        }
    }
    private void UpdateFill()
    {
        float ratio = health.CurrentHealth / health.MaxHealth;
        fillBar.transform.localScale = new Vector3(ratio * xScale, fillBar.transform.localScale.y, fillBar.transform.localScale.z);

        Color color;
        if (ratio > 0.5f)
        {
            float t = (ratio - 0.5f) / 0.5f;
            color = Color.Lerp(Color.yellow, Color.green, t);
        }
        else
        {
            float t = ratio / 0.5f;
            color = Color.Lerp(Color.red, Color.yellow, t);
        }

        fillBar.color = color;
        backgroundBar.color = color * 0.5f;
        /*backgroundLine.color = color * 0.85f;*/

    }

    private void SetVisible(bool visible)
    {
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.enabled = visible;
        }
    }
}
