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

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G) && !isRunningSequence && !isRespondingToScream)
        {
            currentRoutine = StartCoroutine(InspectionSequence());
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

        yield return MoveTo(inspectPoint.position, moveSpeed);

        Debug.Log("Pollora Inspecting");

        isInspecting = true;

        if (playerDetection != null)
        {
            playerDetection.StartInspection();
        }

        yield return new WaitForSeconds(inspectDuration);

        if (playerDetection != null)
        {
            playerDetection.EndInspection();
        }

        isInspecting = false;

        Debug.Log("Pollora Leaving");

        yield return MoveTo(leavePoint.position, moveSpeed);

        Debug.Log("Pollora Gone");

        isRunningSequence = false;
        currentRoutine = null;
    }

    private IEnumerator RespondToScreamSequence()
    {
        isRunningSequence = false;
        isRespondingToScream = true;

        Vector3 screamTargetPosition = GetScreamTargetPosition();

        Debug.Log("Pollora running to last hiding spot check point!");

        yield return MoveTo(screamTargetPosition, runSpeed);

        Debug.Log("Pollora reached scream check point!");

        isInspecting = true;

        if (playerHiding != null && playerHiding.IsHiding)
        {
            GameOverManager.Instance.TriggerGameOver("Stayed in hiding spot after scream");
        }
        else
        {
            Debug.Log("Player escaped hiding spot after scream.");
        }

        yield return new WaitForSeconds(screamInspectDuration);

        isInspecting = false;

        Debug.Log("Pollora Leaving after scream response");

        yield return MoveTo(leavePoint.position, moveSpeed);

        Debug.Log("Pollora Gone");

        isRespondingToScream = false;
        currentRoutine = null;
    }

    private Vector3 GetScreamTargetPosition()
    {
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

    private IEnumerator MoveTo(Vector3 targetPosition, float speed)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                speed * Time.deltaTime
            );

            yield return null;
        }
    }
}