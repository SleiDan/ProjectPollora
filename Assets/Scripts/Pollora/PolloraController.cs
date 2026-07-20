using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public enum PolloraState
{
    Inactive,
    Waiting,
    Approaching,
    Inspecting,
    Leaving,
    RespondingToScream
}

[RequireComponent(typeof(PolloraFootsteps), typeof(NavMeshAgent))]
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
    [SerializeField] private InteractableHidingSpot[] hidingSpots;
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float stoppingDistance = 0.1f;
    [SerializeField] private float navMeshSampleDistance = 3f;
    [SerializeField] private float pathCalculationTimeout = 2f;

    [Header("Inspection")]
    [SerializeField] private float minInspectionDelay = 10f;
    [SerializeField] private float maxInspectionDelay = 20f;
    [SerializeField] private float inspectDuration = 4f;
    [SerializeField] private float screamInspectDuration = 2f;

    [Header("Debug")]
    [SerializeField] private PolloraState currentState = PolloraState.Inactive;
    [SerializeField] private InteractableHidingSpot currentInspectionSpot;

    private Coroutine currentRoutine;
    private NavMeshAgent navMeshAgent;
    private InteractableHidingSpot lastInspectedSpot;
    private InteractableHidingSpot screamHidingSpot;
    private bool lastMovementSucceeded;

    public PolloraState CurrentState => currentState;

    private void Awake()
    {
        if (polloraFootsteps == null)
            polloraFootsteps = GetComponent<PolloraFootsteps>();

        navMeshAgent = GetComponent<NavMeshAgent>();

        if (!HasRequiredReferences())
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {
        PlayerStress.OnPlayerScreamed += HandlePlayerScream;
    }

    private void OnDisable()
    {
        PlayerStress.OnPlayerScreamed -= HandlePlayerScream;
        CancelCurrentRoutine();
        currentState = PolloraState.Inactive;
    }

    private void Start()
    {
        navMeshSurface.BuildNavMesh();

        if (navMeshSurface.navMeshData == null)
        {
            Debug.LogError("Pollora could not build NavMesh data for the scene.", navMeshSurface);
            enabled = false;
            return;
        }

        Vector3 initialPosition = startPoint != null
            ? startPoint.position
            : transform.position;

        if (!TryPlaceOnNavMesh(initialPosition))
        {
            enabled = false;
            return;
        }

        StartAutomaticInspections();
    }

    private void OnValidate()
    {
        moveSpeed = Mathf.Max(0.01f, moveSpeed);
        runSpeed = Mathf.Max(0.01f, runSpeed);
        stoppingDistance = Mathf.Max(0f, stoppingDistance);
        navMeshSampleDistance = Mathf.Max(0.01f, navMeshSampleDistance);
        pathCalculationTimeout = Mathf.Max(0.1f, pathCalculationTimeout);
        minInspectionDelay = Mathf.Max(0f, minInspectionDelay);
        maxInspectionDelay = Mathf.Max(minInspectionDelay, maxInspectionDelay);
        inspectDuration = Mathf.Max(0f, inspectDuration);
        screamInspectDuration = Mathf.Max(0f, screamInspectDuration);
    }

    private bool HasRequiredReferences()
    {
        bool hasHidingSpot = false;

        if (hidingSpots != null)
        {
            for (int i = 0; i < hidingSpots.Length; i++)
            {
                if (hidingSpots[i] != null)
                {
                    hasHidingSpot = true;
                    break;
                }
            }
        }

        if (leavePoint != null &&
            playerDetection != null &&
            playerHiding != null &&
            navMeshSurface != null &&
            navMeshAgent != null &&
            hasHidingSpot)
        {
            return true;
        }

        Debug.LogError(
            "PolloraController requires a Leave Point, Player Detection, Player Hiding, NavMesh Surface, NavMesh Agent, and at least one Hiding Spot.",
            this
        );

        return false;
    }

    private void StartAutomaticInspections()
    {
        if (!isActiveAndEnabled)
            return;

        currentRoutine = StartCoroutine(AutomaticInspectionLoop());
    }

    private IEnumerator AutomaticInspectionLoop()
    {
        while (true)
        {
            currentState = PolloraState.Waiting;

            float delay = Random.Range(minInspectionDelay, maxInspectionDelay);
            float elapsed = 0f;

            while (elapsed < delay)
            {
                if (!IsGameOverActive())
                {
                    elapsed += Time.deltaTime;
                }

                yield return null;
            }

            while (IsGameOverActive())
            {
                yield return null;
            }

            InteractableHidingSpot selectedSpot = SelectRandomHidingSpot();

            if (selectedSpot == null)
            {
                Debug.LogError("Pollora could not select a valid hiding spot.", this);
                currentRoutine = null;
                currentState = PolloraState.Inactive;
                yield break;
            }

            yield return InspectHidingSpot(selectedSpot);
        }
    }

    private IEnumerator InspectHidingSpot(InteractableHidingSpot hidingSpot)
    {
        currentInspectionSpot = hidingSpot;
        lastInspectedSpot = hidingSpot;
        currentState = PolloraState.Approaching;

        Debug.Log("Pollora approaching: " + hidingSpot.gameObject.name);

        yield return MoveTo(hidingSpot.PolloraCheckPosition, moveSpeed, false);

        if (!lastMovementSucceeded)
        {
            currentInspectionSpot = null;
            yield break;
        }

        currentState = PolloraState.Inspecting;
        playerDetection.StartInspection(hidingSpot);

        Debug.Log("Pollora inspecting: " + hidingSpot.gameObject.name);

        yield return new WaitForSeconds(inspectDuration);

        EndActiveInspection();
        currentState = PolloraState.Leaving;

        Debug.Log("Pollora leaving");

        yield return MoveTo(leavePoint.position, moveSpeed, false);

        currentInspectionSpot = null;

        Debug.Log("Pollora gone");
    }

    private InteractableHidingSpot SelectRandomHidingSpot()
    {
        int validCount = 0;
        int alternativeCount = 0;

        for (int i = 0; i < hidingSpots.Length; i++)
        {
            InteractableHidingSpot hidingSpot = hidingSpots[i];

            if (hidingSpot == null)
                continue;

            validCount++;

            if (hidingSpot != lastInspectedSpot)
            {
                alternativeCount++;
            }
        }

        if (validCount == 0)
            return null;

        bool excludePrevious = alternativeCount > 0;
        int targetIndex = Random.Range(0, excludePrevious ? alternativeCount : validCount);

        for (int i = 0; i < hidingSpots.Length; i++)
        {
            InteractableHidingSpot hidingSpot = hidingSpots[i];

            if (hidingSpot == null ||
                (excludePrevious && hidingSpot == lastInspectedSpot))
            {
                continue;
            }

            if (targetIndex == 0)
                return hidingSpot;

            targetIndex--;
        }

        return null;
    }

    private void HandlePlayerScream()
    {
        if (currentState == PolloraState.RespondingToScream || IsGameOverActive())
            return;

        screamHidingSpot = playerHiding.LastHidingSpot;

        Debug.Log("Pollora heard the scream!");

        CancelCurrentRoutine();
        currentRoutine = StartCoroutine(RespondToScreamSequence());
    }

    private IEnumerator RespondToScreamSequence()
    {
        currentState = PolloraState.RespondingToScream;

        Vector3 screamTargetPosition = GetScreamTargetPosition();

        Debug.Log("Pollora running to the scream hiding spot!");

        yield return MoveTo(screamTargetPosition, runSpeed, true);

        if (!lastMovementSucceeded)
        {
            FinishScreamResponse();
            yield break;
        }

        playerDetection.StartInspection(screamHidingSpot);

        if (playerHiding.IsHiding &&
            playerHiding.CurrentHidingSpot == screamHidingSpot)
        {
            GameOverManager.TryTriggerGameOver("Stayed in same hiding spot after scream");
        }
        else
        {
            Debug.Log("Player escaped the compromised hiding spot.");
        }

        yield return new WaitForSeconds(screamInspectDuration);

        EndActiveInspection();
        currentState = PolloraState.Leaving;

        Debug.Log("Pollora leaving after scream response");

        yield return MoveTo(leavePoint.position, moveSpeed, false);

        Debug.Log("Pollora gone");

        FinishScreamResponse();
    }

    private Vector3 GetScreamTargetPosition()
    {
        if (screamHidingSpot != null)
        {
            return screamHidingSpot.PolloraCheckPosition;
        }

        if (playerHiding.LastHidingSpot != null)
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
        lastMovementSucceeded = false;

        if (!navMeshAgent.isOnNavMesh)
        {
            Debug.LogError("Pollora NavMeshAgent is not placed on a NavMesh.", this);
            yield break;
        }

        if (!NavMesh.SamplePosition(
                targetPosition,
                out NavMeshHit targetHit,
                navMeshSampleDistance,
                navMeshAgent.areaMask))
        {
            Debug.LogError($"Pollora could not find NavMesh near destination {targetPosition}.", this);
            yield break;
        }

        navMeshAgent.speed = speed;
        navMeshAgent.stoppingDistance = stoppingDistance;
        navMeshAgent.isStopped = false;

        if (!navMeshAgent.SetDestination(targetHit.position))
        {
            Debug.LogError($"Pollora could not set destination {targetHit.position}.", this);
            StopNavigation();
            yield break;
        }

        float pathWaitTime = 0f;

        while (navMeshAgent.pathPending && pathWaitTime < pathCalculationTimeout)
        {
            pathWaitTime += Time.deltaTime;
            yield return null;
        }

        if (navMeshAgent.pathPending ||
            navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError($"Pollora could not calculate a complete path to {targetHit.position}.", this);
            StopNavigation();
            yield break;
        }

        polloraFootsteps.StartFootsteps(running);

        while (navMeshAgent.remainingDistance > navMeshAgent.stoppingDistance)
        {
            if (!navMeshAgent.hasPath ||
                navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                Debug.LogError("Pollora lost its NavMesh path while moving.", this);
                StopNavigation();
                yield break;
            }

            yield return null;
        }

        lastMovementSucceeded = true;
        StopNavigation();
    }

    private void CancelCurrentRoutine()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }

        StopNavigation();
        EndActiveInspection();
        currentInspectionSpot = null;
    }

    private bool TryPlaceOnNavMesh(Vector3 requestedPosition)
    {
        if (!NavMesh.SamplePosition(
                requestedPosition,
                out NavMeshHit hit,
                navMeshSampleDistance,
                navMeshAgent.areaMask))
        {
            Debug.LogError($"Pollora could not find NavMesh near start position {requestedPosition}.", this);
            return false;
        }

        navMeshAgent.enabled = false;
        transform.position = hit.position;
        navMeshAgent.enabled = true;

        if (!navMeshAgent.isOnNavMesh)
        {
            Debug.LogError($"Pollora could not initialize NavMeshAgent at {hit.position}.", this);
            return false;
        }

        return true;
    }

    private void StopNavigation()
    {
        polloraFootsteps?.StopFootsteps();

        if (navMeshAgent == null ||
            !navMeshAgent.enabled ||
            !navMeshAgent.isOnNavMesh)
        {
            return;
        }

        navMeshAgent.isStopped = true;
        navMeshAgent.ResetPath();
    }

    private void FinishScreamResponse()
    {
        EndActiveInspection();
        screamHidingSpot = null;
        currentRoutine = null;
        StartAutomaticInspections();
    }

    private void EndActiveInspection()
    {
        if (playerDetection != null && playerDetection.IsInspectionActive)
        {
            playerDetection.EndInspection();
        }
    }

    private bool IsGameOverActive()
    {
        return GameOverManager.Instance != null && GameOverManager.Instance.IsGameOver;
    }
}
