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

        transform.SetPositionAndRotation(
            hidingSpot.HidePoint.position,
            hidingSpot.HidePoint.rotation
        );

        characterController.enabled = true;
    }

    public void ExitHidingSpot()
    {
        if (!isHiding)
            return;

        isHiding = false;
        currentHidingSpot = null;

        playerController.enabled = true;
    }

    public void ForceExitHiding()
    {
        isHiding = false;
        currentHidingSpot = null;

        if (playerController != null)
            playerController.enabled = true;
    }
}
