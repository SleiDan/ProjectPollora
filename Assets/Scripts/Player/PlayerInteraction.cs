using TMPro;
using UnityEngine;

[RequireComponent(typeof(PlayerHiding))]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private PlayerHiding playerHiding;

    [Header("UI")]
    [SerializeField] private TMP_Text interactionText;

    private InteractableHidingSpot currentTarget;

    private void Awake()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerHiding == null)
            playerHiding = GetComponent<PlayerHiding>();

        HideInteractionText();

        if (playerCamera == null)
        {
            Debug.LogError(
                "PlayerInteraction requires a player Camera. Assign one in the Inspector or tag it as MainCamera.",
                this
            );

            enabled = false;
        }
    }

    private void Update()
    {
        if (playerHiding.IsHiding)
        {
            ShowInteractionText("[E] Exit");

            if (Input.GetKeyDown(KeyCode.E))
            {
                playerHiding.ExitHidingSpot();
            }

            return;
        }

        FindInteractable();

        if (currentTarget != null)
        {
            ShowInteractionText("[E] Hide");

            if (Input.GetKeyDown(KeyCode.E))
            {
                currentTarget.Interact(playerHiding);
            }
        }
        else
        {
            HideInteractionText();
        }
    }

    private void FindInteractable()
    {
        currentTarget = null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            currentTarget = hit.collider.GetComponent<InteractableHidingSpot>();
        }
    }

    private void ShowInteractionText(string text)
    {
        if (interactionText == null)
            return;

        interactionText.text = text;
        interactionText.gameObject.SetActive(true);
    }

    private void HideInteractionText()
    {
        if (interactionText == null)
            return;

        interactionText.gameObject.SetActive(false);
    }
}
