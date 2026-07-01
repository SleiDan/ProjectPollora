using UnityEngine;

public class PlayerScreamAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip screamClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;
    [SerializeField] [Range(0.5f, 1.2f)] private float pitch = 0.85f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
        }
    }

    private void OnEnable()
    {
        PlayerStress.OnPlayerScreamed += PlayScream;
    }

    private void OnDisable()
    {
        PlayerStress.OnPlayerScreamed -= PlayScream;
    }

    private void PlayScream()
    {
        if (audioSource == null || screamClip == null)
            return;

        audioSource.pitch = pitch;
        audioSource.PlayOneShot(screamClip, volume);
    }
}