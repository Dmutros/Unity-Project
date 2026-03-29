using UnityEngine;

public class GoreFadeOut : MonoBehaviour
{
    public float lifeTime = 3f;
    public float fadeTime = 1f;

    private float timer;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        timer = lifeTime;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer < fadeTime)
        {
            float alpha = timer / fadeTime;
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }

        if (timer <= 0)
        {
            Destroy(gameObject);
        }
    }
}
