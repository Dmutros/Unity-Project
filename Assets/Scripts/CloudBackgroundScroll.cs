using UnityEngine;

public class CloudBackgroundScroll : MonoBehaviour
{
    public GameObject cloudTilePrefab;
    public float tileWidth = 20f;
    public float scrollSpeed = 2f;

    private GameObject tileA;
    private GameObject tileB;

    void Start()
    {
        tileA = Instantiate(cloudTilePrefab, transform.position, Quaternion.identity, transform);
        tileB = Instantiate(cloudTilePrefab, transform.position + Vector3.right * tileWidth, Quaternion.identity, transform);
    }

    void Update()
    {
        float move = scrollSpeed * Time.deltaTime;
        tileA.transform.position -= Vector3.right * move;
        tileB.transform.position -= Vector3.right * move;

        // Коли один тайл виходить за межу — переносимо його вправо
        if (tileA.transform.position.x <= -tileWidth)
        {
            tileA.transform.position += Vector3.right * tileWidth * 2;
            SwapTiles();
        }

        if (tileB.transform.position.x <= -tileWidth)
        {
            tileB.transform.position += Vector3.right * tileWidth * 2;
            SwapTiles();
        }
    }

    void SwapTiles()
    {
        var temp = tileA;
        tileA = tileB;
        tileB = temp;
    }
}
