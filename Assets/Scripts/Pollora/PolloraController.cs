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

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    [Header("Inspection")]
    [SerializeField] private float inspectDuration = 4f;

    private bool isRunningSequence;

    private void Start()
    {
        if (startPoint != null)
        {
            transform.position = startPoint.position;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G) && !isRunningSequence)
        {
            StartCoroutine(InspectionSequence());
        }
    }

    private IEnumerator InspectionSequence()
    {
        isRunningSequence = true;

        Debug.Log("Pollora Approaching");

        yield return MoveTo(inspectPoint.position);

        Debug.Log("Pollora Inspecting");

        if (playerDetection != null)
        {
            playerDetection.StartInspection();
        }

        yield return new WaitForSeconds(inspectDuration);

        if (playerDetection != null)
        {
            playerDetection.EndInspection();
        }

        Debug.Log("Pollora Leaving");

        yield return MoveTo(leavePoint.position);

        Debug.Log("Pollora Gone");

        isRunningSequence = false;
    }

    private IEnumerator MoveTo(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPosition,
                moveSpeed * Time.deltaTime
            );

            yield return null;
        }
    }
}