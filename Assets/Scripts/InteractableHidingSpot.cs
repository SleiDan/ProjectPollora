using UnityEngine;

public class InteractableHidingSpot : MonoBehaviour
{
    [SerializeField] private Transform hidePoint;
    [SerializeField] private Transform polloraCheckPoint;

    public Transform HidePoint => hidePoint;
    public Transform PolloraCheckPoint => polloraCheckPoint;

    public Vector3 PolloraCheckPosition
    {
        get
        {
            if (polloraCheckPoint != null)
                return polloraCheckPoint.position;

            if (hidePoint != null)
                return hidePoint.position;

            return transform.position;
        }
    }

    public void Interact(PlayerHiding playerHiding)
    {
        if (playerHiding == null)
        {
            Debug.LogError("Cannot use a hiding spot without a PlayerHiding component.", this);
            return;
        }

        if (hidePoint == null)
        {
            Debug.LogError("Cannot use this hiding spot because its Hide Point is not assigned.", this);
            return;
        }

        playerHiding.EnterHidingSpot(this);
    }
}
