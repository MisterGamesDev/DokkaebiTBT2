using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Dokkaebi.Core;

namespace Dokkaebi.UI
{
    /// <summary>
    /// Handles the game over UI display
    /// </summary>
    public class GameOverUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Button restartButton;

        [Header("Messages")]
        [SerializeField] private string player1WinsMessage = "VICTORY!";
        [SerializeField] private string player2WinsMessage = "DEFEAT!";
        
        private GameController gameController;
        
        private void Awake()
        {
            // Get references
            if (gameController == null)
            {
                gameController = FindObjectOfType<GameController>();
            }
            
            // Hide panel initially
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
            
            // Set up restart button
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(RestartGame);
            }
        }
        
        private void Start()
        {
            // Subscribe to game over event
            if (gameController != null)
            {
                gameController.OnGameOver += HandleGameOver;
            }
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (gameController != null)
            {
                gameController.OnGameOver -= HandleGameOver;
            }
            
            // Clean up button listener
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(RestartGame);
            }
        }
        
        /// <summary>
        /// Handle game over event
        /// </summary>
        private void HandleGameOver(bool player1Wins)
        {
            // Update message text
            if (messageText != null)
            {
                messageText.text = player1Wins ? player1WinsMessage : player2WinsMessage;
            }
            
            // Show the panel
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// Restart the game
        /// </summary>
        private void RestartGame()
        {
            // Hide the panel
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
            
            // Reset game state
            if (gameController != null)
            {
                gameController.ResetGame();
            }
            
            // For prototype, reload the scene
            // In a full game, you would likely have a more robust reset mechanism
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }
} 