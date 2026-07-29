using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PolloraController), typeof(NavMeshAgent))]
public sealed class PolloraAnimator : MonoBehaviour
{
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int LookingAroundHash = Animator.StringToHash("LookingAround");

    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent navMeshAgent;
    [SerializeField] private PolloraController polloraController;
    [SerializeField] private float dampingTime = 0.12f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>(true);

        if (navMeshAgent == null)
            navMeshAgent = GetComponent<NavMeshAgent>();

        if (polloraController == null)
            polloraController = GetComponent<PolloraController>();
    }

    private void Update()
    {
        if (animator == null || navMeshAgent == null || polloraController == null)
            return;

        float moveSpeed = navMeshAgent.enabled && navMeshAgent.isOnNavMesh
            ? navMeshAgent.velocity.magnitude
            : 0f;

        animator.SetFloat(MoveSpeedHash, moveSpeed, dampingTime, Time.deltaTime);
        animator.SetBool(
            LookingAroundHash,
            polloraController.CurrentState == PolloraState.LookingAround
        );
    }
}
