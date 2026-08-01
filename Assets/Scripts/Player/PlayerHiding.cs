using UnityEngine;

[RequireComponent(typeof(PlayerController), typeof(CharacterController))]
public class PlayerHiding : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterController characterController;

    private InteractableHidingSpot currentHidingSpot;
    private InteractableHidingSpot lastHidingSpot;

    private bool isHiding;

    public bool IsHiding => isHiding;
    public InteractableHidingSpot CurrentHidingSpot => currentHidingSpot;
    public InteractableHidingSpot LastHidingSpot => lastHidingSpot;

    public Vector3 LastHidingPosition
    {
        get
        {
            if (lastHidingSpot != null && lastHidingSpot.HidePoint != null)
                return lastHidingSpot.HidePoint.position;

            return transform.position;
        }
    }

    public Vector3 LastPolloraCheckPosition
    {
        get
        {
            if (lastHidingSpot != null)
                return lastHidingSpot.PolloraCheckPosition;

            return transform.position;
        }
    }

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    public void EnterHidingSpot(InteractableHidingSpot hidingSpot)
    {
        if (isHiding)
            return;

        if (hidingSpot == null || hidingSpot.HidePoint == null)
        {
            Debug.LogError("Cannot enter a hiding spot without a valid Hide Point.", this);
            return;
        }

        currentHidingSpot = hidingSpot;
        lastHidingSpot = hidingSpot;

        isHiding = true;

        playerController.enabled = false;
        characterController.enabled = false;

        Quaternion viewRotation = GetOutwardViewRotation(hidingSpot);
        transform.SetPositionAndRotation(hidingSpot.HidePoint.position, viewRotation);

        playerController.ResetViewRotation();
    }

    private static Quaternion GetOutwardViewRotation(InteractableHidingSpot hidingSpot)
    {
        Vector3 lookDirection = hidingSpot.ExitPoint.position - hidingSpot.HidePoint.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude < 0.0001f)
        {
            lookDirection = hidingSpot.HidePoint.forward;
            lookDirection.y = 0f;
        }

        return lookDirection.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up)
            : hidingSpot.HidePoint.rotation;
    }

    public void ExitHidingSpot()
    {
        if (!isHiding)
            return;

        InteractableHidingSpot hidingSpot = currentHidingSpot;

        isHiding = false;
        currentHidingSpot = null;

        if (hidingSpot != null && hidingSpot.ExitPoint != null)
        {
            transform.SetPositionAndRotation(
                hidingSpot.ExitPoint.position,
                GetOutwardViewRotation(hidingSpot)
            );
        }

        characterController.enabled = true;

        playerController.enabled = true;
    }

    public void ForceExitHiding()
    {
        isHiding = false;
        currentHidingSpot = null;

        if (characterController != null)
            characterController.enabled = true;

        if (playerController != null)
            playerController.enabled = true;
    }

    public void ResetForNewAttempt()
    {
        ForceExitHiding();
        lastHidingSpot = null;
    }
}
