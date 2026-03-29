using UnityEngine;

public class CamController : MonoBehaviour
{
    public float moveSpeed;
    [Range (0, 1)]
    public float smoothTime;

    public Transform playerTransform;

    [HideInInspector]
    public int worldSizeH;
    [HideInInspector]
    public float worldSizeW;
    float orthoSize;
    float camHalfWidth;
    GameObject playerObject;
    public void Spawn(Vector3 spawnPos)
    {
        playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            GetComponent<Transform>().position = spawnPos;
        }

        orthoSize = GetComponent<Camera>().orthographicSize;
        //worldSizeW = worldSizeW * 0.08f;
    }
    public void FixedUpdate()
    {
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        Vector3 pos = GetComponent<Transform>().position;

        pos.x = Mathf.Lerp(pos.x, playerTransform.position.x, smoothTime);
        pos.y = Mathf.Lerp(pos.y, playerTransform.position.y, smoothTime);
        camHalfWidth = orthoSize * Camera.main.aspect;

        pos.x = Mathf.Clamp(pos.x, camHalfWidth, (worldSizeW*0.08f - camHalfWidth));
        pos.y = Mathf.Clamp(pos.y, orthoSize, worldSizeH - orthoSize);
        /*        pos.x = Mathf.Clamp(pos.x, 0 + (orthoSize *2), worldSizeW - (orthoSize*2));*/

        GetComponent<Transform>().position = pos;
        }
    }
}
/*        pos.x = playerTransform.position.x;
        pos.y = playerTransform.position.y;*/