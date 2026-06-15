using UnityEngine;

public class PlayerStress : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerEyes playerEyes;

    [Header("Stress Settings")]
    [SerializeField] private float maxStress = 100f;
    [SerializeField] private float stressIncreaseSpeed = 25f;
    [SerializeField] private float stressDecreaseSpeed = 15f;

    [Header("Debug")]
    [SerializeField] private float currentStress;

    public float CurrentStress => currentStress;
    public float MaxStress => maxStress;

    private void Awake()
    {
        if (playerEyes == null)
            playerEyes = GetComponent<PlayerEyes>();
    }

    private void Update()
    {
        UpdateStress();
    }

    private void UpdateStress()
    {
        if (playerEyes.IsClosingEyes)
        {
            IncreaseStress();
        }
        else
        {
            DecreaseStress();
        }
    }

    private void IncreaseStress()
    {
        currentStress += stressIncreaseSpeed * Time.deltaTime;
        currentStress = Mathf.Clamp(currentStress, 0f, maxStress);
    }

    private void DecreaseStress()
    {
        currentStress -= stressDecreaseSpeed * Time.deltaTime;
        currentStress = Mathf.Clamp(currentStress, 0f, maxStress);
    }
}