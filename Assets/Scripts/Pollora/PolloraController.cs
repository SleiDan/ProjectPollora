using System.Collections;
using UnityEngine;

public class PolloraController : MonoBehaviour
{
    [Header("Points")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform inspectPoint;
    [SerializeField] private Transform leavePoint;

    [Header("References")]
    [SerializeField] private PlayerDetection playerDetection;
    [SerializeField] private PlayerHiding playerHiding;
    [SerializeField] private PolloraFootsteps polloraFootsteps;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;

    [Header("Inspection")]
    [SerializeField] private float inspectDuration = 4f;
    [SerializeField] private float screamInspectDuration = 2f;

    [Header("Debug")]
    [SerializeField] private bool screamDetected;
    [SerializeField] private bool isInspecting;
    [SerializeField] private bool isRespondingToScream;

    private bool isRunningSequence;
    private Coroutine currentRoutine;

    private InteractableHidingSpot screamHidingSpot;

    private void Awake()
    {
        if (polloraFootsteps == null)
            polloraFootsteps = GetComponent<PolloraFootsteps>();
    }

    private void OnEnable()
    {
        PlayerStress.OnPlayerScreamed += HandlePlayerScream;
    }

    private void OnDisable()
    {
        PlayerStress.OnPlayerScreamed -= HandlePlayerScream;
    }

    private void Start()
    {
        if (startPoint != null)
        {
            transform.position = startPoint.position;
        }
    }

    private void HandlePlayerScream()
    {
        screamDetected = true;
        Debug.Log("Pollora heard the scream!");

        if (isInspecting)
        {
            GameOverManager.Instance.TriggerGameOver("Screamed during inspection");
            return;
        }

        if (playerHiding != null)
        {
            screamHidingSpot = playerHiding.LastHidingSpot;
        }

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        currentRoutine = StartCoroutine(RespondToScreamSequence());
    }

    private IEnumerator InspectionSequence()
    {
        isRunningSequence = true;
        screamDetected = false;

        Debug.Log("Pollora Approaching");

        yield return MoveTo(inspectPoint.position, moveSpeed, false);

        Debug.Log("Pollora Inspecting");

        isInspecting = true;

        if (playerDetection != null && playerHiding != null)
        {
            playerDetection.StartInspection(playerHiding.LastHidingSpot);
        }

        yield return new WaitForSeconds(inspectDuration);

        if (playerDetection != null)
        {
            playerDetection.EndInspection();
        }

        isInspecting = false;

        Debug.Log("Pollora Leaving");

        yield return MoveTo(leavePoint.position, moveSpeed, false);

        Debug.Log("Pollora Gone");

        isRunningSequence = false;
        currentRoutine = null;
    }

    private IEnumerator RespondToScreamSequence()
    {
        isRunningSequence = false;
        isRespondingToScream = true;

        Vector3 screamTargetPosition = GetScreamTargetPosition();

        Debug.Log("Pollora running to scream hiding spot check point!");

        yield return MoveTo(screamTargetPosition, runSpeed, true);

        Debug.Log("Pollora reached scream check point!");

        isInspecting = true;

        if (playerDetection != null)
        {
            playerDetection.StartInspection(screamHidingSpot);
        }

        if (playerHiding != null &&
            playerHiding.IsHiding &&
            playerHiding.CurrentHidingSpot == screamHidingSpot)
        {
            GameOverManager.Instance.TriggerGameOver("Stayed in same hiding spot after scream");
        }
        else
        {
            Debug.Log("Player escaped the compromised hiding spot.");
        }

        yield return new WaitForSeconds(screamInspectDuration);

        if (playerDetection != null)
        {
            playerDetection.EndInspection();
        }

        isInspecting = false;

        Debug.Log("Pollora Leaving after scream response");

        yield return MoveTo(leavePoint.position, moveSpeed, false);

        Debug.Log("Pollora Gone");

        isRespondingToScream = false;
        screamHidingSpot = null;
        currentRoutine = null;
    }

    private Vector3 GetScreamTargetPosition()
    {
        if (screamHidingSpot != null)
        {
            return screamHidingSpot.PolloraCheckPosition;
        }

        if (playerHiding != null)
        {
            return playerHiding.LastPolloraCheckPosition;
        }

        if (inspectPoint != null)
        {
            return inspectPoint.position;
        }

        return transform.position;
    }

    private IEnumerator MoveTo(Vector3 targetPosition, float speed, bool running)
    {
        if (polloraFootsteps != null)
        {
            polloraFootsteps.StartFootsteps(running);
        }

        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime
            );

            yield return null;
        }

        if (polloraFootsteps != null)
        {
            polloraFootsteps.StopFootsteps();
        }
    }
}