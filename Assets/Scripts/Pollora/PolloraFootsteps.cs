using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PolloraFootsteps : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip footstepClip;

    [Header("Step Timing")]
    [SerializeField] private float walkStepInterval = 0.55f;
    [SerializeField] private float runStepInterval = 0.28f;

    [Header("Volume")]
    [SerializeField] private float walkVolume = 0.6f;
    [SerializeField] private float runVolume = 0.9f;

    private float stepTimer;
    private bool isMoving;
    private bool isRunning;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.pitch = 1f;
        }
    }

    private void Update()
    {
        if (!isMoving || audioSource == null || footstepClip == null)
            return;

        float interval = isRunning ? runStepInterval : walkStepInterval;
        float volume = isRunning ? runVolume : walkVolume;

        stepTimer += Time.deltaTime;

        if (stepTimer >= interval)
        {
            audioSource.PlayOneShot(footstepClip, volume);
            stepTimer = 0f;
        }
    }

    public void StartFootsteps(bool running)
    {
        isMoving = true;
        isRunning = running;
        stepTimer = 0f;
    }

    public void StopFootsteps()
    {
        isMoving = false;
        stepTimer = 0f;
    }
}
