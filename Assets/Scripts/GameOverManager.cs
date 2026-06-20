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

    [Header("Settings")]
    [SerializeField] private float respawnDelay = 2f;

    [Header("Debug")]
    [SerializeField] private bool isGameOver;

    public bool IsGameOver => isGameOver;

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
    }

    public void TriggerGameOver(string reason)
    {
        if (isGameOver)
            return;

        Debug.Log($"GAME OVER: {reason}");
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        isGameOver = true;

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

        yield return new WaitForSeconds(respawnDelay);

        RespawnPlayer();

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

        isGameOver = false;

        Debug.Log("Player respawned.");
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