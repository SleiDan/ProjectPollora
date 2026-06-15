using UnityEngine;

public class PlayerDetection : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerEyes playerEyes;

    [Header("Detection")]
    [SerializeField] private float maxDetection = 100f;
    [SerializeField] private float detectionSpeed = 35f;

    [Header("Debug")]
    [SerializeField] private float currentDetection;

    private bool inspectionActive;

    public bool IsDetected => currentDetection >= maxDetection;

    private void Awake()
    {
        if (playerEyes == null)
            playerEyes = GetComponent<PlayerEyes>();
    }

    private void Update()
    {
        if (!inspectionActive)
            return;

        if (!playerEyes.IsClosingEyes)
        {
            currentDetection += detectionSpeed * Time.deltaTime;
            currentDetection = Mathf.Clamp(currentDetection, 0f, maxDetection);
        }

        if (currentDetection >= maxDetection)
        {
            Debug.Log("PLAYER DETECTED!");
        }
    }

    public void StartInspection()
    {
        currentDetection = 0f;
        inspectionActive = true;

        Debug.Log("Inspection Started");
    }

    public void EndInspection()
    {
        inspectionActive = false;

        Debug.Log("Inspection Ended");
    }
}