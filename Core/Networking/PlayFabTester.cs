using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Dokkaebi.Core.Networking
{
    /// <summary>
    /// Utility class for testing PlayFab integration
    /// </summary>
    public class PlayFabTester : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private NetworkingManager networkManager;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private Button loginButton;
        [SerializeField] private Button testCommandButton;

        private void Awake()
        {
            if (networkManager == null)
                networkManager = FindObjectOfType<NetworkingManager>();
        }

        private void Start()
        {
            // Subscribe to events
            if (networkManager != null)
            {
                networkManager.OnLoginSuccess += HandleLoginSuccess;
                networkManager.OnLoginFailure += HandleLoginFailure;
                networkManager.OnNetworkError += HandleNetworkError;
                networkManager.OnGameStateUpdated += HandleGameStateUpdate;
            }

            // Set up button listeners
            if (loginButton != null)
                loginButton.onClick.AddListener(HandleLoginButtonClick);

            if (testCommandButton != null)
            {
                testCommandButton.onClick.AddListener(HandleTestCommandClick);
                testCommandButton.interactable = false; // Disabled until logged in
            }

            UpdateStatusText("Ready to test PlayFab integration");
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            if (networkManager != null)
            {
                networkManager.OnLoginSuccess -= HandleLoginSuccess;
                networkManager.OnLoginFailure -= HandleLoginFailure;
                networkManager.OnNetworkError -= HandleNetworkError;
                networkManager.OnGameStateUpdated -= HandleGameStateUpdate;
            }

            // Remove button listeners
            if (loginButton != null)
                loginButton.onClick.RemoveListener(HandleLoginButtonClick);

            if (testCommandButton != null)
                testCommandButton.onClick.RemoveListener(HandleTestCommandClick);
        }

        private void HandleLoginButtonClick()
        {
            if (networkManager != null)
            {
                UpdateStatusText("Attempting to login...");
                networkManager.LoginWithDeviceId();
            }
            else
            {
                UpdateStatusText("ERROR: NetworkingManager not found!");
            }
        }

        private void HandleTestCommandClick()
        {
            if (networkManager != null && networkManager.IsAuthenticated())
            {
                UpdateStatusText("Testing CloudScript execution...");
                
                // Create a simple test CloudScript call
                var testData = new Dictionary<string, object>
                {
                    { "testParam", "Hello from Dokkaebi!" },
                    { "timestamp", System.DateTime.UtcNow.ToString() }
                };

                networkManager.ExecuteCloudScript(
                    "HelloWorld", // Function name - you need to create this in PlayFab
                    testData,
                    result => {
                        UpdateStatusText("CloudScript executed successfully!");
                        Debug.Log("CloudScript result: " + result);
                    },
                    error => {
                        UpdateStatusText("CloudScript ERROR: " + error.ErrorMessage);
                        Debug.LogError("CloudScript error: " + error.ErrorMessage);
                    }
                );
            }
            else
            {
                UpdateStatusText("ERROR: Not logged in to PlayFab!");
            }
        }

        private void HandleLoginSuccess()
        {
            UpdateStatusText("Login SUCCESS!");
            
            if (testCommandButton != null)
                testCommandButton.interactable = true;
        }

        private void HandleLoginFailure(string error)
        {
            UpdateStatusText("Login FAILED: " + error);
            
            if (testCommandButton != null)
                testCommandButton.interactable = false;
        }

        private void HandleNetworkError(string error)
        {
            UpdateStatusText("Network ERROR: " + error);
        }

        private void HandleGameStateUpdate(Dictionary<string, object> gameState)
        {
            UpdateStatusText("Game state updated!");
            Debug.Log("Received game state with " + gameState.Count + " entries");
        }

        private void UpdateStatusText(string message)
        {
            Debug.Log("[PlayFabTester] " + message);
            
            if (statusText != null)
                statusText.text = message;
        }
    }
} 
