using UnityEngine;

public class PlayerHiding : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CharacterController characterController;

    private InteractableHidingSpot currentHidingSpot;
    private bool isHiding;

    public bool IsHiding => isHiding;

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

        currentHidingSpot = hidingSpot;
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
}   