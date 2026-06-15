using UnityEngine;

public class InteractableHidingSpot : MonoBehaviour
{
    [SerializeField] private Transform hidePoint;

    public Transform HidePoint => hidePoint;

    public void Interact(PlayerHiding playerHiding)
    {
        playerHiding.EnterHidingSpot(this);
    }
}