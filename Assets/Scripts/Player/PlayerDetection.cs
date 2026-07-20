using UnityEngine;

[RequireComponent(typeof(PlayerEyes), typeof(PlayerHiding))]
public class PlayerDetection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerEyes playerEyes;
    [SerializeField] private PlayerHiding playerHiding;

    [Header("Detection")]
    [SerializeField] private float maxDetection = 100f;
    [SerializeField] private float detectionSpeed = 35f;

    [Header("Debug")]
    [SerializeField] private float currentDetection;
    [SerializeField] private bool inspectionActive;
    [SerializeField] private InteractableHidingSpot inspectedHidingSpot;

    private bool hasLoggedSafeHidingSpot;

    public bool IsDetected => currentDetection >= maxDetection;
    public bool IsInspectionActive => inspectionActive;

    private void Awake()
    {
        if (playerEyes == null)
            playerEyes = GetComponent<PlayerEyes>();

        if (playerHiding == null)
            playerHiding = GetComponent<PlayerHiding>();
    }

    private void Update()
    {
        if (!inspectionActive)
            return;

        if (!IsPlayerInInspectedHidingSpot())
        {
            currentDetection = 0f;
            return;
        }

        if (!playerEyes.IsClosingEyes)
        {
            currentDetection += detectionSpeed * Time.deltaTime;
            currentDetection = Mathf.Clamp(currentDetection, 0f, maxDetection);
        }
        else
        {
            currentDetection = 0f;
        }

        if (currentDetection >= maxDetection)
        {
            GameOverManager.TryTriggerGameOver("Detected during correct hiding spot inspection");
        }
    }

    public void StartInspection(InteractableHidingSpot hidingSpot)
    {
        inspectedHidingSpot = hidingSpot;
        currentDetection = 0f;
        hasLoggedSafeHidingSpot = false;
        inspectionActive = true;

        Debug.Log("Inspection Started. Inspecting: " + GetHidingSpotName(inspectedHidingSpot));
    }

    public void EndInspection()
    {
        inspectionActive = false;
        inspectedHidingSpot = null;
        currentDetection = 0f;
        hasLoggedSafeHidingSpot = false;

        Debug.Log("Inspection Ended");
    }

    private bool IsPlayerInInspectedHidingSpot()
    {
        if (playerHiding == null)
            return false;

        if (!playerHiding.IsHiding)
            return false;

        if (playerHiding.CurrentHidingSpot == null)
            return false;

        if (inspectedHidingSpot == null)
            return false;

        bool sameSpot = playerHiding.CurrentHidingSpot == inspectedHidingSpot;

        if (!sameSpot && !hasLoggedSafeHidingSpot)
        {
            hasLoggedSafeHidingSpot = true;

            Debug.Log(
                "Player is safe. Pollora inspects: " +
                GetHidingSpotName(inspectedHidingSpot) +
                ", player is in: " +
                GetHidingSpotName(playerHiding.CurrentHidingSpot)
            );
        }

        return sameSpot;
    }

    private string GetHidingSpotName(InteractableHidingSpot hidingSpot)
    {
        if (hidingSpot == null)
            return "NULL";

        return hidingSpot.gameObject.name;
    }
}
