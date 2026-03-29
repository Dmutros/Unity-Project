using System.Collections;
using UnityEngine;

public class MeleeWeapon : MonoBehaviour
{
    public WeaponData weaponData;
    public Collider2D weaponCollider;

    private bool isSwinging;

    public Transform player;
    public SpriteRenderer spriteRenderer;

    public float attackCooldown => weaponData.swingDuration;
    private float attackTimer = 0f;
    CharacterScript playerScript;
    private bool flippedForAttack = false;
    public SpriteRenderer playerSpriteRenderer;

    public Transform weaponPivot;

    private void Start()
    {
        spriteRenderer.enabled = false;
        weaponCollider.enabled = false;

        playerScript = GetComponentInParent<CharacterScript>();
    }

    void Update()
    {
        attackTimer += Time.deltaTime;


    }

    public void Attack()
    {
        if (attackTimer >= attackCooldown && !isSwinging)
        {
            attackTimer = 0f;

            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            float dir = Mathf.Sign(mousePos.x - player.transform.position.x);


            /*            Vector3 scale = weaponPivot.transform.localScale;
                        scale.x = Mathf.Abs(scale.x) * dir;
                        weaponPivot.transform.localScale = scale;*/

            weaponPivot.localEulerAngles = new Vector3(0f, dir < 0 ? 180f : 0f, 0f);

            playerScript.Flip(dir < 0);

            StartCoroutine(SwingCoroutine());
        }
    }

    IEnumerator SwingCoroutine()
    {
        isSwinging = true;
        playerScript.SetAttacking(true);
        playerScript.SwingSound();

        float dir = Mathf.Sign(Camera.main.ScreenToWorldPoint(Input.mousePosition).x - transform.parent.position.x);

        float start = weaponData.swingStartAngle;
        float end = weaponData.swingEndAngle;

        if (dir < 0)
        {
            float temp = start;
            start = -end;
            end = -temp;
        }

        float timer = 0f;
        spriteRenderer.enabled = true;
        weaponCollider.enabled = true;

        while (timer < weaponData.swingDuration)
        {
            timer += Time.deltaTime;
            float t = timer / weaponData.swingDuration;
            float angle = Mathf.Lerp(start, end, t);
            transform.localEulerAngles = new Vector3(0, 0, angle);
            yield return null;
        }

        spriteRenderer.enabled = false;
        weaponCollider.enabled = false;
        isSwinging = false;
        playerScript.SetAttacking(false);
    }

    public void ColDamage(Collider2D collision)
    {
        HealthComponent target = collision.gameObject.GetComponent<HealthComponent>();
        if (target != null && target != GetComponentInParent<HealthComponent>())
        {
            Vector2 knockDir = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;

            knockDir.y = 0.5f;
            knockDir.Normalize();

            target.TakeDamage(weaponData.damage, knockDir, weaponData.knockbackForce);
        }
    }
}
