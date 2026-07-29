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
    RespondingToScream,
    Chasing,
    Patrolling,
    LookingAround,
    Searching
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

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float minPatrolPause = 1f;
    [SerializeField] private float maxPatrolPause = 3f;
    [SerializeField] [Range(0f, 1f)] private float hidingSpotInspectionChance = 0.35f;

    [Header("Vision")]
    [SerializeField] [Range(1f, 30f)] private float visionDistance = 12f;
    [SerializeField] [Range(0f, 360f)] private float visionAngle = 90f;
    [SerializeField] private float eyeHeight = 1.6f;
    [SerializeField] private float visionDetectionTime = 0.5f;
    [SerializeField] private LayerMask visionLayers = ~0;
    [SerializeField] private bool showVisionCone = true;

    [Header("Chase")]
    [SerializeField] private float chaseCatchDistance = 1.2f;
    [SerializeField] private float chasePathRefreshInterval = 0.15f;
    [SerializeField] [Range(0f, 10f)] private float chaseLostSightDuration = 2f;
    [SerializeField] [Range(0f, 10f)] private float lookAroundDuration = 3f;

    [Header("Hearing")]
    [SerializeField] [Range(1f, 30f)] private float runningHearingDistance = 15f;
    [SerializeField] [Range(0f, 10f)] private float runningNoiseMemoryDuration = 2.5f;

    [Header("Debug")]
    [SerializeField] private PolloraState currentState = PolloraState.Inactive;
    [SerializeField] private InteractableHidingSpot currentInspectionSpot;
    [SerializeField] private bool canSeePlayer;

    private Coroutine currentRoutine;
    private NavMeshAgent navMeshAgent;
    private InteractableHidingSpot lastInspectedSpot;
    private Transform lastPatrolPoint;
    private InteractableHidingSpot screamHidingSpot;
    private bool lastMovementSucceeded;
    private float playerVisibleTime;
    private CharacterController playerCharacterController;
    private PlayerController playerController;
    private GameObject visionConeObject;
    private Material visionConeMaterial;
    private Mesh visionConeMesh;
    private bool runSuspended;
    private Vector3 lastKnownPlayerPosition;
    private float runningNoiseMemory;

    public PolloraState CurrentState => currentState;

    private void Awake()
    {
        if (polloraFootsteps == null)
            polloraFootsteps = GetComponent<PolloraFootsteps>();

        navMeshAgent = GetComponent<NavMeshAgent>();
        playerCharacterController = playerHiding != null
            ? playerHiding.GetComponent<CharacterController>()
            : null;
        playerController = playerHiding != null
            ? playerHiding.GetComponent<PlayerController>()
            : null;

        if (!HasRequiredReferences())
        {
            enabled = false;
        }
    }

    private void OnEnable()
    {
        PlayerStress.OnPlayerScreamed += HandlePlayerScream;

        if (playerController != null)
            playerController.OnRunningNoise += HandleRunningNoise;
    }

    private void OnDisable()
    {
        PlayerStress.OnPlayerScreamed -= HandlePlayerScream;

        if (playerController != null)
            playerController.OnRunningNoise -= HandleRunningNoise;

        CancelCurrentRoutine();
        ResetVision();
        currentState = PolloraState.Inactive;
    }

    private void Update()
    {
        if (runSuspended)
            return;

        runningNoiseMemory = Mathf.Max(0f, runningNoiseMemory - Time.deltaTime);
        UpdateVision();
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

        CreateVisionCone();
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
        minPatrolPause = Mathf.Max(0f, minPatrolPause);
        maxPatrolPause = Mathf.Max(minPatrolPause, maxPatrolPause);
        visionDistance = Mathf.Max(0f, visionDistance);
        eyeHeight = Mathf.Max(0f, eyeHeight);
        visionDetectionTime = Mathf.Max(0f, visionDetectionTime);
        chaseCatchDistance = Mathf.Max(0.1f, chaseCatchDistance);
        chasePathRefreshInterval = Mathf.Max(0.05f, chasePathRefreshInterval);
        chaseLostSightDuration = Mathf.Max(0f, chaseLostSightDuration);
        lookAroundDuration = Mathf.Max(0f, lookAroundDuration);
        runningHearingDistance = Mathf.Max(1f, runningHearingDistance);
        runningNoiseMemoryDuration = Mathf.Max(0f, runningNoiseMemoryDuration);

        if (Application.isPlaying && visionConeObject != null)
        {
            RebuildVisionCone();
        }
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
        if (!isActiveAndEnabled || runSuspended)
            return;

        currentRoutine = StartCoroutine(AutomaticInspectionLoop());
    }

    private IEnumerator AutomaticInspectionLoop()
    {
        while (true)
        {
            Transform patrolPoint = SelectRandomPatrolPoint();

            if (patrolPoint == null)
            {
                yield return WaitForGameplaySeconds(Random.Range(minInspectionDelay, maxInspectionDelay));
            }
            else
            {
                currentState = PolloraState.Patrolling;
                lastPatrolPoint = patrolPoint;

                Debug.Log("Pollora patrolling to: " + patrolPoint.gameObject.name, this);
                yield return MoveTo(patrolPoint.position, moveSpeed, false);

                if (lastMovementSucceeded)
                {
                    yield return WaitForGameplaySeconds(Random.Range(minPatrolPause, maxPatrolPause));
                }
            }

            if (Random.value > hidingSpotInspectionChance)
                continue;

            InteractableHidingSpot selectedSpot = SelectRandomHidingSpot();

            if (selectedSpot == null)
            {
                Debug.LogError("Pollora could not select a valid hiding spot.", this);
                continue;
            }

            yield return InspectHidingSpot(selectedSpot, false);
        }
    }

    private IEnumerator WaitForGameplaySeconds(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (!IsGameOverActive())
                elapsed += Time.deltaTime;

            yield return null;
        }
    }

    private IEnumerator InspectHidingSpot(InteractableHidingSpot hidingSpot, bool leaveAfterInspection)
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
        currentInspectionSpot = null;

        if (!leaveAfterInspection)
            yield break;

        currentState = PolloraState.Leaving;
        Debug.Log("Pollora leaving");
        yield return MoveTo(leavePoint.position, moveSpeed, false);
        Debug.Log("Pollora gone");
    }

    private Transform SelectRandomPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return null;

        int validCount = 0;
        int alternativeCount = 0;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Transform patrolPoint = patrolPoints[i];

            if (patrolPoint == null)
                continue;

            validCount++;

            if (patrolPoint != lastPatrolPoint)
                alternativeCount++;
        }

        if (validCount == 0)
            return null;

        bool excludePrevious = alternativeCount > 0;
        int targetIndex = Random.Range(0, excludePrevious ? alternativeCount : validCount);

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            Transform patrolPoint = patrolPoints[i];

            if (patrolPoint == null ||
                (excludePrevious && patrolPoint == lastPatrolPoint))
            {
                continue;
            }

            if (targetIndex == 0)
                return patrolPoint;

            targetIndex--;
        }

        return null;
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

    private void UpdateVision()
    {
        if (currentState == PolloraState.Chasing)
            return;

        bool sawPlayerThisFrame = CanSeePlayer();

        if (sawPlayerThisFrame && !canSeePlayer)
        {
            Debug.Log("Pollora saw the player!", this);
        }

        canSeePlayer = sawPlayerThisFrame;
        UpdateVisionConeColor();

        if (!canSeePlayer)
        {
            playerVisibleTime = 0f;
            return;
        }

        playerVisibleTime += Time.deltaTime;

        if (playerVisibleTime < visionDetectionTime)
            return;

        playerVisibleTime = 0f;
        StartChase();
    }

    private void StartChase()
    {
        if (currentState == PolloraState.Chasing || IsGameOverActive())
            return;

        if (runningNoiseMemory <= 0f && playerHiding != null)
            lastKnownPlayerPosition = playerHiding.transform.position;

        Debug.Log("Pollora started chasing the player!", this);

        CancelCurrentRoutine();
        currentState = PolloraState.Chasing;
        canSeePlayer = true;
        UpdateVisionConeColor();
        currentRoutine = StartCoroutine(ChasePlayer());
    }

    private IEnumerator ChasePlayer()
    {
        if (!navMeshAgent.isOnNavMesh)
        {
            Debug.LogError("Pollora cannot chase because its NavMeshAgent is not on a NavMesh.", this);
            FinishChase();
            yield break;
        }

        navMeshAgent.speed = runSpeed;
        navMeshAgent.stoppingDistance = chaseCatchDistance;
        navMeshAgent.isStopped = false;
        polloraFootsteps.StartFootsteps(true);

        float pathRefreshTimer = chasePathRefreshInterval;
        float lostSightTimer = 0f;
        bool lostPlayer = false;

        while (!IsGameOverActive())
        {
            if (playerHiding == null)
            {
                break;
            }

            canSeePlayer = CanSeePlayer();
            UpdateVisionConeColor();

            if (canSeePlayer)
            {
                lostSightTimer = 0f;
                lastKnownPlayerPosition = playerHiding.transform.position;
            }
            else if (runningNoiseMemory > 0f)
            {
                lostSightTimer = 0f;
            }
            else
            {
                lostSightTimer += Time.deltaTime;

                if (lostSightTimer >= chaseLostSightDuration)
                {
                    Debug.Log("Pollora stopped chasing after losing sight of the player.", this);
                    lostPlayer = true;
                    break;
                }
            }

            Vector3 toPlayer = lastKnownPlayerPosition - transform.position;
            toPlayer.y = 0f;

            if (canSeePlayer &&
                toPlayer.sqrMagnitude <= chaseCatchDistance * chaseCatchDistance)
            {
                StopNavigation();
                Debug.Log("Pollora caught the player!", this);
                GameOverManager.TryTriggerGameOver("Caught by Pollora");

                while (IsGameOverActive())
                {
                    yield return null;
                }

                break;
            }

            pathRefreshTimer += Time.deltaTime;

            if (pathRefreshTimer >= chasePathRefreshInterval)
            {
                pathRefreshTimer = 0f;

                if (NavMesh.SamplePosition(
                        lastKnownPlayerPosition,
                        out NavMeshHit playerHit,
                        navMeshSampleDistance,
                        navMeshAgent.areaMask))
                {
                    navMeshAgent.SetDestination(playerHit.position);
                }
            }

            yield return null;
        }

        StopNavigation();

        if (lostPlayer)
        {
            currentState = PolloraState.LookingAround;
            Debug.Log("Pollora looking around after losing the player.", this);

            if (lookAroundDuration > 0f)
                yield return new WaitForSeconds(lookAroundDuration);

            currentState = PolloraState.Searching;
            InteractableHidingSpot nearestHidingSpot = FindNearestHidingSpotByPath(out float pathDistance);

            if (nearestHidingSpot != null)
            {
                Debug.Log(
                    $"Pollora searching {nearestHidingSpot.gameObject.name}; NavMesh path length: {pathDistance:F1}",
                    this
                );

                yield return InspectHidingSpot(nearestHidingSpot, false);
            }

            FinishChase();
            yield break;
        }

        currentState = PolloraState.Leaving;
        yield return MoveTo(leavePoint.position, moveSpeed, false);
        FinishChase();
    }

    private InteractableHidingSpot FindNearestHidingSpotByPath(out float shortestDistance)
    {
        shortestDistance = float.PositiveInfinity;
        InteractableHidingSpot nearestHidingSpot = null;
        NavMeshPath candidatePath = new NavMeshPath();

        for (int i = 0; i < hidingSpots.Length; i++)
        {
            InteractableHidingSpot hidingSpot = hidingSpots[i];

            if (hidingSpot == null ||
                !NavMesh.SamplePosition(
                    hidingSpot.PolloraCheckPosition,
                    out NavMeshHit targetHit,
                    navMeshSampleDistance,
                    navMeshAgent.areaMask))
            {
                continue;
            }

            if (!NavMesh.CalculatePath(
                    transform.position,
                    targetHit.position,
                    navMeshAgent.areaMask,
                    candidatePath) ||
                candidatePath.status != NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            float candidateDistance = CalculatePathLength(candidatePath);

            if (candidateDistance >= shortestDistance)
                continue;

            shortestDistance = candidateDistance;
            nearestHidingSpot = hidingSpot;
        }

        return nearestHidingSpot;
    }

    private float CalculatePathLength(NavMeshPath path)
    {
        float length = 0f;
        Vector3[] corners = path.corners;

        for (int i = 1; i < corners.Length; i++)
        {
            length += Vector3.Distance(corners[i - 1], corners[i]);
        }

        return length;
    }

    private void FinishChase()
    {
        StopNavigation();
        ResetVision();
        currentRoutine = null;
        StartAutomaticInspections();
    }

    private bool CanSeePlayer()
    {
        if (runSuspended ||
            playerHiding == null ||
            playerHiding.IsHiding ||
            IsGameOverActive())
        {
            return false;
        }

        Vector3 eyePosition = GetEyePosition();
        Vector3 playerTarget = playerCharacterController != null
            ? playerCharacterController.bounds.center
            : playerHiding.transform.position + Vector3.up;
        Vector3 toPlayer = playerTarget - eyePosition;
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer <= Mathf.Epsilon ||
            distanceToPlayer > visionDistance)
        {
            return false;
        }

        Vector3 directionToPlayer = toPlayer / distanceToPlayer;

        if (Vector3.Angle(transform.forward, directionToPlayer) > visionAngle * 0.5f)
            return false;

        if (!Physics.Raycast(
                eyePosition,
                directionToPlayer,
                out RaycastHit hit,
                distanceToPlayer,
                visionLayers,
                QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        Transform hitTransform = hit.transform;
        Transform playerTransform = playerHiding.transform;

        return hitTransform == playerTransform ||
               hitTransform.IsChildOf(playerTransform);
    }

    private void HandleRunningNoise(Vector3 noisePosition)
    {
        if (runSuspended ||
            IsGameOverActive() ||
            (noisePosition - transform.position).sqrMagnitude >
            runningHearingDistance * runningHearingDistance)
        {
            return;
        }

        bool newlyHeard = runningNoiseMemory <= 0f;
        lastKnownPlayerPosition = noisePosition;
        runningNoiseMemory = runningNoiseMemoryDuration;

        if (newlyHeard)
            Debug.Log("Pollora heard the player running!", this);

        if (currentState != PolloraState.Chasing)
            StartChase();
    }

    private Vector3 GetEyePosition()
    {
        return transform.position + Vector3.up * eyeHeight;
    }

    private void ResetVision()
    {
        canSeePlayer = false;
        playerVisibleTime = 0f;
        UpdateVisionConeColor();
    }

    private void CreateVisionCone()
    {
        if (!showVisionCone || visionConeObject != null)
            return;

        const int segmentCount = 32;
        Vector3[] vertices = new Vector3[segmentCount + 2];
        int[] triangles = new int[segmentCount * 3];
        float halfAngle = visionAngle * 0.5f;

        vertices[0] = Vector3.up * 0.05f;

        for (int i = 0; i <= segmentCount; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segmentCount);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            vertices[i + 1] = direction * visionDistance + Vector3.up * 0.05f;
        }

        for (int i = 0; i < segmentCount; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }

        visionConeMesh = new Mesh
        {
            name = "Pollora Vision Cone"
        };
        visionConeMesh.vertices = vertices;
        visionConeMesh.triangles = triangles;
        visionConeMesh.RecalculateNormals();
        visionConeMesh.RecalculateBounds();

        visionConeObject = new GameObject("Vision Cone (Debug)");
        visionConeObject.transform.SetParent(transform, false);

        MeshFilter meshFilter = visionConeObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = visionConeMesh;

        MeshRenderer meshRenderer = visionConeObject.AddComponent<MeshRenderer>();
        Shader coneShader = Shader.Find("Sprites/Default");

        if (coneShader == null)
        {
            coneShader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (coneShader == null)
        {
            Debug.LogWarning("Pollora vision cone shader was not found.", this);
            visionConeObject.SetActive(false);
            return;
        }

        visionConeMaterial = new Material(coneShader)
        {
            name = "Pollora Vision Cone (Debug)"
        };
        meshRenderer.sharedMaterial = visionConeMaterial;
        UpdateVisionConeColor();
    }

    public void SetVisionDistance(float distance)
    {
        visionDistance = Mathf.Clamp(distance, 1f, 30f);
        RebuildVisionCone();
    }

    public void SetPatrolPoints(Transform[] points)
    {
        patrolPoints = points;
    }

    public void PauseForRunEnd()
    {
        runSuspended = true;
        CancelCurrentRoutine();
        ResetVision();
        currentState = PolloraState.Inactive;
    }

    public bool ResetForNewAttempt()
    {
        PauseForRunEnd();

        lastInspectedSpot = null;
        lastPatrolPoint = null;
        screamHidingSpot = null;
        lastMovementSucceeded = false;
        runningNoiseMemory = 0f;
        lastKnownPlayerPosition = Vector3.zero;

        Vector3 resetPosition = startPoint != null
            ? startPoint.position
            : transform.position;

        if (!TryPlaceOnNavMesh(resetPosition))
            return false;

        if (startPoint != null)
            transform.rotation = startPoint.rotation;

        runSuspended = false;
        StartAutomaticInspections();
        return true;
    }

    private void RebuildVisionCone()
    {
        if (visionConeObject != null)
        {
            Destroy(visionConeObject);
            visionConeObject = null;
        }

        if (visionConeMaterial != null)
        {
            Destroy(visionConeMaterial);
            visionConeMaterial = null;
        }

        if (visionConeMesh != null)
        {
            Destroy(visionConeMesh);
            visionConeMesh = null;
        }

        CreateVisionCone();
    }

    private void UpdateVisionConeColor()
    {
        if (visionConeMaterial == null)
            return;

        visionConeMaterial.color = canSeePlayer
            ? new Color(1f, 0f, 0f, 0.4f)
            : new Color(1f, 0.85f, 0f, 0.25f);
    }

    private void OnDestroy()
    {
        if (visionConeMaterial != null)
        {
            Destroy(visionConeMaterial);
        }

        if (visionConeMesh != null)
        {
            Destroy(visionConeMesh);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eyePosition = GetEyePosition();
        float halfAngle = visionAngle * 0.5f;
        Vector3 leftBoundary = Quaternion.Euler(0f, -halfAngle, 0f) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0f, halfAngle, 0f) * transform.forward;

        Gizmos.color = canSeePlayer ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(eyePosition, 0.08f);
        Gizmos.DrawRay(eyePosition, leftBoundary * visionDistance);
        Gizmos.DrawRay(eyePosition, rightBoundary * visionDistance);

        const int segmentCount = 24;
        Vector3 previousPoint = eyePosition + leftBoundary * visionDistance;

        for (int i = 1; i <= segmentCount; i++)
        {
            float angle = Mathf.Lerp(-halfAngle, halfAngle, i / (float)segmentCount);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * transform.forward;
            Vector3 nextPoint = eyePosition + direction * visionDistance;
            Gizmos.DrawLine(previousPoint, nextPoint);
            previousPoint = nextPoint;
        }
    }
}
