using System;
using UnityEngine;

public enum StressState
{
    Calm,
    Anxious,
    Panicked,
    Overloaded
}

[RequireComponent(typeof(PlayerEyes))]
public class PlayerStress : MonoBehaviour
{
    public static event Action OnPlayerScreamed;

    [Header("References")]
    [SerializeField] private PlayerEyes playerEyes;

    [Header("Stress Settings")]
    [SerializeField] private float maxStress = 100f;
    [SerializeField] private float stressIncreaseSpeed = 25f;
    [SerializeField] private float stressDecreaseSpeed = 15f;

    [Header("Debug")]
    [SerializeField] private float currentStress;
    [SerializeField] private StressState currentStressState;

    private StressState previousStressState;
    private bool hasScreamed;

    public float CurrentStress => currentStress;
    public float MaxStress => maxStress;
    public StressState CurrentStressState => currentStressState;

    private void Awake()
    {
        if (playerEyes == null)
            playerEyes = GetComponent<PlayerEyes>();

        previousStressState = currentStressState;
    }

    private void Update()
    {
        UpdateStress();
    }

    private void UpdateStress()
    {
        if (playerEyes.IsClosingEyes)
            IncreaseStress();
        else
            DecreaseStress();

        UpdateStressState();
        CheckOverload();
        CheckRecovery();
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

    private void UpdateStressState()
    {
        if (currentStress <= 0f)
        {
            currentStressState = StressState.Calm;
        }
        else if (currentStress < maxStress * 0.33f)
        {
            currentStressState = StressState.Anxious;
        }
        else if (currentStress < maxStress * 0.66f)
        {
            currentStressState = StressState.Panicked;
        }
        else
        {
            currentStressState = StressState.Overloaded;
        }

        if (currentStressState != previousStressState)
        {
            Debug.Log($"Stress State Changed: {currentStressState}");
            previousStressState = currentStressState;
        }
    }

    private void CheckOverload()
    {
        if (currentStress >= maxStress && !hasScreamed)
        {
            hasScreamed = true;

            Debug.Log("PLAYER SCREAMED!");

            if (playerEyes != null)
            {
                playerEyes.SetCanCloseEyes(false);
            }

            OnPlayerScreamed?.Invoke();
        }
    }

    private void CheckRecovery()
    {
        if (hasScreamed && currentStressState == StressState.Calm)
        {
            hasScreamed = false;

            if (playerEyes != null)
            {
                playerEyes.SetCanCloseEyes(true);
            }

            Debug.Log("Player recovered from scream state. Can close eyes again.");
        }
    }

    public void ResetStress()
    {
        currentStress = 0f;
        currentStressState = StressState.Calm;
        previousStressState = currentStressState;
        hasScreamed = false;

        if (playerEyes != null)
        {
            playerEyes.SetCanCloseEyes(true);
            playerEyes.ForceOpenEyes();
        }

        Debug.Log("Stress reset.");
    }
}
