using UnityEngine;
using UnityEngine.UI;

public class PlayerEyes : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHiding playerHiding;
    [SerializeField] private Image eyesClosedOverlay;

    [Header("Settings")]
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private float closedAlpha = 1f;

    private bool isClosingEyes;
    private float currentAlpha;

    public bool IsClosingEyes => isClosingEyes;

    private void Awake()
    {
        if (playerHiding == null)
            playerHiding = GetComponent<PlayerHiding>();

        SetOverlayAlpha(0f);
    }

    private void Update()
    {
        HandleInput();
        UpdateOverlay();
    }

    private void HandleInput()
    {
        if (!playerHiding.IsHiding)
        {
            isClosingEyes = false;
            return;
        }

        isClosingEyes = Input.GetMouseButton(1);
    }

    private void UpdateOverlay()
    {
        float targetAlpha = isClosingEyes ? closedAlpha : 0f;

        currentAlpha = Mathf.MoveTowards(
            currentAlpha,
            targetAlpha,
            fadeSpeed * Time.deltaTime
        );

        SetOverlayAlpha(currentAlpha);
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (eyesClosedOverlay == null)
            return;

        Color color = eyesClosedOverlay.color;
        color.a = alpha;
        eyesClosedOverlay.color = color;
    }
}