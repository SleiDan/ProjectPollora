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
        playerHiding.EnterHidingSpot(this);
    }
}