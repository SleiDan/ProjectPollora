using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text gameOverText;

    [SerializeField] private float fadeSpeed = 2f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        SetAlpha(0f);

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }
    }

    public void ShowGameOver()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    public void HideGameOver()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(HideRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(true);
        }

        float alpha = 0f;

        while (alpha < 1f)
        {
            alpha += Time.deltaTime * fadeSpeed;

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(1f);
    }

    private IEnumerator HideRoutine()
    {
        float alpha = 1f;

        while (alpha > 0f)
        {
            alpha -= Time.deltaTime * fadeSpeed;

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(0f);

        if (gameOverText != null)
        {
            gameOverText.gameObject.SetActive(false);
        }
    }

    private void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);

        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = alpha;
            backgroundImage.color = bgColor;
        }

        if (gameOverText != null)
        {
            Color textColor = gameOverText.color;
            textColor.a = alpha;
            gameOverText.color = textColor;
        }
    }
}