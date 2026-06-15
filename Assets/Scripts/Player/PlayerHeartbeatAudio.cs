using UnityEngine;

public class PlayerHeartbeatAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStress playerStress;
    [SerializeField] private AudioSource heartbeatSource;
    [SerializeField] private AudioClip heartbeatClip;

    [Header("Volume")]
    [SerializeField] private float minVolume = 0f;
    [SerializeField] private float maxVolume = 1f;

    [Header("Beat Timing")]
    [SerializeField] private float slowestBeatInterval = 1.2f;
    [SerializeField] private float fastestBeatInterval = 0.2f;

    [Header("Curve")]
    [SerializeField] private float curvePower = 2f;

    private float beatTimer;

    private void Awake()
    {
        if (playerStress == null)
            playerStress = GetComponent<PlayerStress>();
    }

    private void Update()
    {
        if (playerStress == null ||
            heartbeatSource == null ||
            heartbeatClip == null)
        {
            return;
        }

        float stressPercent =
            playerStress.CurrentStress / playerStress.MaxStress;

        stressPercent = Mathf.Clamp01(stressPercent);

        float curvedStress =
            Mathf.Pow(stressPercent, curvePower);

        float targetVolume =
            Mathf.Lerp(minVolume, maxVolume, curvedStress);

        heartbeatSource.volume = targetVolume;

        if (stressPercent <= 0f)
            return;

        float beatInterval =
            Mathf.Lerp(
                slowestBeatInterval,
                fastestBeatInterval,
                curvedStress
            );

        beatTimer += Time.deltaTime;

        if (beatTimer >= beatInterval)
        {
            heartbeatSource.PlayOneShot(
                heartbeatClip,
                targetVolume
            );

            beatTimer = 0f;
        }
    }
}