using UnityEngine;

public class FootstepSound : MonoBehaviour
{
    public AudioClip[] footstepSounds;
    public AudioSource footstepSource;
    public CharacterScript characterScript;
    public float stepInterval = 0.4f;
    private float stepTimer;

    void Update()
    {
        if (characterScript.onGround && Mathf.Abs(characterScript.horizontal) > 0.1f && !characterScript.GetIsAttacking())
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                PlayFootstep();
                stepTimer = stepInterval;
            }
        }
        else
        {
            stepTimer = 0f;
        }
    }

    void PlayFootstep()
    {
        if (footstepSounds.Length == 0) return;

        int index = Random.Range(0, footstepSounds.Length);
        footstepSource.pitch = Random.Range(0.95f, 1.05f);
        footstepSource.PlayOneShot(footstepSounds[index]);
    }
}
