using UnityEngine;

[RequireComponent(typeof(Collider))]
public class LevelGoal : MonoBehaviour
{
    private bool triggered;
    private Collider goalCollider;
    private CharacterController playerController;

    private void Awake()
    {
        goalCollider = GetComponent<Collider>();
        PlayerController player = FindAnyObjectByType<PlayerController>();

        if (player != null)
            playerController = player.GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!triggered &&
            goalCollider != null &&
            playerController != null &&
            goalCollider.bounds.Intersects(playerController.bounds))
        {
            CompleteLevel();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggered || other.GetComponentInParent<PlayerController>() == null)
            return;

        CompleteLevel();
    }

    private void CompleteLevel()
    {
        if (GameOverManager.TryCompleteLevel())
        {
            triggered = true;
        }
    }

    public void ResetGoal()
    {
        triggered = false;
    }
}
