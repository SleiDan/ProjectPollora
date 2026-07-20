using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PolloraBreathAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;

    [Header("Settings")]
    [SerializeField] private float maxVolume = 0.9f;
    [SerializeField] private float fadeSpeed = 2f;

    [Header("Head Movement")]
    [SerializeField] private float swayDistance = 0.08f;
    [SerializeField] private float swaySpeed = 0.6f;

    private Vector3 startLocalPosition;
    private Coroutine fadeRoutine;
    private bool isPlaying;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        startLocalPosition = transform.localPosition;

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = 0f;
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        float offset = Mathf.Sin(Time.time * swaySpeed) * swayDistance;

        transform.localPosition =
            startLocalPosition + transform.right * offset;
    }

    public void StartBreathing()
    {
        isPlaying = true;

        if (!audioSource.isPlaying)
            audioSource.Play();

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeVolume(maxVolume));
    }

    public void StopBreathing()
    {
        isPlaying = false;

        transform.localPosition = startLocalPosition;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeOut());
    }

    private IEnumerator FadeVolume(float targetVolume)
    {
        while (audioSource.volume < targetVolume)
        {
            audioSource.volume += Time.deltaTime * fadeSpeed;

            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    private IEnumerator FadeOut()
    {
        while (audioSource.volume > 0f)
        {
            audioSource.volume -= Time.deltaTime * fadeSpeed;

            yield return null;
        }

        audioSource.volume = 0f;

        audioSource.Stop();
    }
}
