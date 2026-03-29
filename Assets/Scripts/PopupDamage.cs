using TMPro;
using UnityEngine;

public class PopupDamage : MonoBehaviour
{
    public TextMeshPro text;
    private float moveSpeed = 0.08f;
    private float disappearTime = 0.5f;
    private Color textColor;
    private Vector3 moveDirection;

    public void Setup(int damageAmount)
    {
        text.text = damageAmount.ToString();
        textColor = text.color;
        moveDirection = new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0f);

        float randomAngle = Random.Range(-20f, 20f);
        transform.rotation = Quaternion.Euler(0f, 0f, randomAngle);

        Destroy(gameObject, disappearTime);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        renderer.sortingOrder = 1;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        textColor.a -= Time.deltaTime / disappearTime;
        text.color = textColor;
    }
}
