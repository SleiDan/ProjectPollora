using System.Collections;
using UnityEngine;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerHiding playerHiding;
    [SerializeField] private PlayerEyes playerEyes;
    [SerializeField] private PlayerStress playerStress;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private PolloraController polloraController;
    [SerializeField] private LevelGoal levelGoal;

    [Header("Settings")]
    [SerializeField] private float respawnDelay = 3.5f;

    [Header("Debug")]
    [SerializeField] private bool isGameOver;
    [SerializeField] private bool isLevelComplete;

    public bool IsGameOver => isGameOver;
    public bool IsRunEnded => isGameOver || isLevelComplete;

    public static bool TryTriggerGameOver(string reason)
    {
        if (Instance == null)
        {
            Debug.LogError($"Cannot trigger Game Over because no {nameof(GameOverManager)} exists in the scene. Reason: {reason}");
            return false;
        }

        Instance.TriggerGameOver(reason);
        return true;
    }

    public static bool TryCompleteLevel()
    {
        if (Instance == null)
        {
            Debug.LogError($"Cannot complete the level because no {nameof(GameOverManager)} exists in the scene.");
            return false;
        }

        Instance.CompleteLevel();
        return true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (player != null)
        {
            if (playerController == null)
                playerController = player.GetComponent<PlayerController>();

            if (playerHiding == null)
                playerHiding = player.GetComponent<PlayerHiding>();

            if (playerEyes == null)
                playerEyes = player.GetComponent<PlayerEyes>();

            if (playerStress == null)
                playerStress = player.GetComponent<PlayerStress>();

            if (characterController == null)
                characterController = player.GetComponent<CharacterController>();
        }

        if (gameOverUI == null)
        {
            gameOverUI = FindAnyObjectByType<GameOverUI>();
        }

        if (polloraController == null)
        {
            polloraController = FindAnyObjectByType<PolloraController>();
        }

        if (levelGoal == null)
        {
            levelGoal = FindAnyObjectByType<LevelGoal>();
        }
    }

    private void Update()
    {
        if (isLevelComplete && Input.GetKeyDown(KeyCode.R))
        {
            RestartAttempt();
        }
    }

    public void TriggerGameOver(string reason)
    {
        if (IsRunEnded)
            return;

        Debug.Log($"GAME OVER: {reason}");
        StartCoroutine(GameOverRoutine());
    }

    public void CompleteLevel()
    {
        if (IsRunEnded)
            return;

        isLevelComplete = true;
        PauseRun();

        if (gameOverUI != null)
        {
            gameOverUI.ShowMessage("YOU ESCAPED\n\nPress R to restart");
        }

        Debug.Log("LEVEL COMPLETE");
    }

    private IEnumerator GameOverRoutine()
    {
        isGameOver = true;

        if (gameOverUI != null)
        {
            gameOverUI.ShowGameOver();
        }

        PauseRun();

        yield return new WaitForSeconds(respawnDelay);

        RestartAttempt();
    }

    private void PauseRun()
    {
        if (playerEyes != null)
        {
            playerEyes.SetCanCloseEyes(false);
            playerEyes.ForceOpenEyes();
        }

        if (playerHiding != null)
        {
            playerHiding.ForceExitHiding();
        }

        if (playerController != null)
            playerController.enabled = false;

        polloraController?.PauseForRunEnd();
    }

    public void RestartAttempt()
    {
        RespawnPlayer();

        if (playerHiding != null)
        {
            playerHiding.ResetForNewAttempt();
        }

        if (playerStress != null)
        {
            playerStress.ResetStress();
        }

        if (playerEyes != null)
        {
            playerEyes.SetCanCloseEyes(true);
            playerEyes.ForceOpenEyes();
        }

        if (playerController != null)
            playerController.enabled = true;

        if (gameOverUI != null)
        {
            gameOverUI.HideGameOver();
        }

        isGameOver = false;
        isLevelComplete = false;
        levelGoal?.ResetGoal();

        if (polloraController != null &&
            !polloraController.ResetForNewAttempt())
        {
            Debug.LogError("Pollora could not reset for the new attempt.", polloraController);
        }

        Debug.Log("New attempt started.");
    }

    private void RespawnPlayer()
    {
        if (player == null || respawnPoint == null)
            return;

        if (characterController != null)
            characterController.enabled = false;

        player.SetPositionAndRotation(
            respawnPoint.position,
            respawnPoint.rotation
        );

        if (characterController != null)
            characterController.enabled = true;
    }
}
