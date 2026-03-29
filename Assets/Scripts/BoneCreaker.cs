using UnityEngine;

public class BoneCreaker : MonoBehaviour
{
    public AudioClip creakSound;
    public AudioSource audioSource; 
    public float minDelay = 4f; 
    public float maxDelay = 12f; 

    private float timer;

    void Start()
    {
        ResetTimer();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            PlayCreak();
            ResetTimer();
        }
    }

    void ResetTimer()
    {
        timer = Random.Range(minDelay, maxDelay);
    }

    void PlayCreak()
    {
        if (creakSound == null) return;

        audioSource.pitch = Random.Range(0.95f, 1.05f);
        audioSource.PlayOneShot(creakSound);
    }
}
