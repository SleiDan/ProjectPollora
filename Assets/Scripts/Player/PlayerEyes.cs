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
    private bool canCloseEyes = true;
    private float currentAlpha;

    public bool IsClosingEyes => isClosingEyes;
    public bool CanCloseEyes => canCloseEyes;

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
        if (!playerHiding.IsHiding || !canCloseEyes)
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

    public void ForceOpenEyes()
    {
        isClosingEyes = false;
    }

    public void SetCanCloseEyes(bool value)
    {
        canCloseEyes = value;

        if (!canCloseEyes)
        {
            ForceOpenEyes();
        }
    }
}   