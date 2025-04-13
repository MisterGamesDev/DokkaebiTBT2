using System;
using System.Collections.Generic;
using UnityEngine;
// Uncomment PlayFab namespace references
using PlayFab;
using PlayFab.ClientModels;

namespace Dokkaebi.Core.Networking
{
    /// <summary>
    /// Handles network communication with PlayFab backend services
    /// Acts as wrapper for PlayFab SDK calls to enable authoritative server communication
    /// </summary>
    public class NetworkingManager : MonoBehaviour
    {
        public static NetworkingManager Instance { get; private set; }

        [Header("PlayFab Settings")]
        [SerializeField] private string titleId;
        [SerializeField] private bool useDeviceId = true;

        // Session and authentication state
        private string playFabId;
        private string sessionTicket;
        private bool isAuthenticated = false;

        // Match state
        private string currentMatchId;
        private string currentMatchGroup;

        // Events
        public event Action OnLoginSuccess;
        public event Action<string> OnLoginFailure;
        public event Action<string> OnNetworkError;
        public event Action<Dictionary<string, object>> OnGameStateUpdated;

        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Set PlayFab Title ID
            if (!string.IsNullOrEmpty(titleId))
            {
                PlayFabSettings.TitleId = titleId;
            }
        }

        private void Start()
        {
            // Auto login on start if configured to do so
            if (useDeviceId)
            {
                LoginWithDeviceId();
            }
        }

        #region Authentication

        /// <summary>
        /// Login to PlayFab using device ID (for testing)
        /// </summary>
        public void LoginWithDeviceId()
        {
            // Generate a unique device ID if it doesn't exist
            string deviceId = GetOrCreateDeviceId();

            var request = new LoginWithCustomIDRequest
            {
                CustomId = deviceId,
                CreateAccount = true
            };

            PlayFabClientAPI.LoginWithCustomID(
                request,
                OnLoginSuccess_Internal,
                OnLoginFailure_Internal
            );

            Debug.Log($"Logging in with device ID: {deviceId}");
        }

        /// <summary>
        /// Get stored device ID or create a new one
        /// </summary>
        private string GetOrCreateDeviceId()
        {
            const string KEY_DEVICE_ID = "PLAYFAB_DEVICE_ID";
            string deviceId = PlayerPrefs.GetString(KEY_DEVICE_ID, "");

            if (string.IsNullOrEmpty(deviceId))
            {
                deviceId = SystemInfo.deviceUniqueIdentifier;
                if (string.IsNullOrEmpty(deviceId) || deviceId == SystemInfo.unsupportedIdentifier)
                {
                    deviceId = Guid.NewGuid().ToString();
                }

                PlayerPrefs.SetString(KEY_DEVICE_ID, deviceId);
                PlayerPrefs.Save();
            }

            return deviceId;
        }

        private void OnLoginSuccess_Internal(LoginResult result)
        {
            playFabId = result.PlayFabId;
            sessionTicket = result.SessionTicket;
            isAuthenticated = true;

            Debug.Log($"PlayFab login successful. ID: {playFabId}");
            OnLoginSuccess?.Invoke();
        }

        private void OnLoginFailure_Internal(PlayFabError error)
        {
            isAuthenticated = false;
            string errorMessage = error.GenerateErrorReport();
            Debug.LogError($"PlayFab login failed: {errorMessage}");
            OnLoginFailure?.Invoke(errorMessage);
        }

        /// <summary>
        /// Check if the user is currently authenticated with PlayFab
        /// </summary>
        public bool IsAuthenticated()
        {
            return isAuthenticated;
        }

        #endregion

        #region CloudScript Function Calls

        /// <summary>
        /// Execute a PlayFab CloudScript/Azure Function with the given name and parameters
        /// </summary>
        public void ExecuteCloudScript(
            string functionName, 
            object parameters, 
            Action<ExecuteCloudScriptResult> onSuccess = null, 
            Action<PlayFabError> onError = null)
        {
            if (!IsAuthenticated())
            {
                Debug.LogError("Cannot execute CloudScript: Not authenticated with PlayFab");
                onError?.Invoke(new PlayFabError
                {
                    Error = PlayFabErrorCode.NotAuthenticated,
                    ErrorMessage = "Not authenticated with PlayFab"
                });
                return;
            }

            var request = new ExecuteCloudScriptRequest
            {
                FunctionName = functionName,
                FunctionParameter = parameters,
                GeneratePlayStreamEvent = true
            };

            PlayFabClientAPI.ExecuteCloudScript(
                request,
                result => {
                    if (result.FunctionResult != null)
                    {
                        Debug.Log($"CloudScript function {functionName} executed successfully");
                        onSuccess?.Invoke(result);
                    }
                    else
                    {
                        Debug.LogWarning($"CloudScript function {functionName} returned null result");
                        onSuccess?.Invoke(result);
                    }
                },
                error => {
                    string errorMessage = error.GenerateErrorReport();
                    Debug.LogError($"CloudScript function {functionName} execution failed: {errorMessage}");
                    onError?.Invoke(error);
                    OnNetworkError?.Invoke(errorMessage);
                }
            );
        }

        #endregion

        #region Game State Management

        /// <summary>
        /// Fetch the current game state from the server
        /// </summary>
        public void GetGameState(Action<Dictionary<string, object>> onStateReceived = null)
        {
            if (string.IsNullOrEmpty(currentMatchId))
            {
                Debug.LogWarning("Cannot get game state: No active match");
                return;
            }

            ExecuteCloudScript(
                "GetGameState",
                new { matchId = currentMatchId },
                result => {
                    try
                    {
                        // Parse the game state from the function result
                        var gameState = result.FunctionResult as Dictionary<string, object>;
                        if (gameState != null)
                        {
                            OnGameStateUpdated?.Invoke(gameState);
                            onStateReceived?.Invoke(gameState);
                        }
                        else
                        {
                            Debug.LogError("Failed to parse game state from result");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing game state: {ex.Message}");
                    }
                }
            );
        }

        /// <summary>
        /// Set the current match ID and shared group ID
        /// </summary>
        public void SetCurrentMatch(string matchId, string sharedGroupId)
        {
            currentMatchId = matchId;
            currentMatchGroup = sharedGroupId;
            Debug.Log($"Current match set to: {matchId}");
        }

        /// <summary>
        /// Get the current match ID
        /// </summary>
        public string GetCurrentMatchId()
        {
            return currentMatchId;
        }

        /// <summary>
        /// Get the current match shared group ID
        /// </summary>
        public string GetCurrentMatchGroupId()
        {
            return currentMatchGroup;
        }

        #endregion

        #region Command Execution

        /// <summary>
        /// Execute a game command on the server
        /// </summary>
        public void ExecuteCommand(
            string commandName, 
            object commandData, 
            Action<Dictionary<string, object>> onSuccess = null,
            Action<string> onError = null)
        {
            if (string.IsNullOrEmpty(currentMatchId))
            {
                string errorMsg = "Cannot execute command: No active match";
                Debug.LogWarning(errorMsg);
                onError?.Invoke(errorMsg);
                return;
            }

            // Add match ID to command data
            var fullCommandData = new Dictionary<string, object>
            {
                { "matchId", currentMatchId },
                { "commandData", commandData }
            };

            // Execute the command on the server
            ExecuteCloudScript(
                commandName,
                fullCommandData,
                result => {
                    try
                    {
                        var responseData = result.FunctionResult as Dictionary<string, object>;
                        if (responseData != null)
                        {
                            // Check for success flag in response
                            if (responseData.TryGetValue("success", out object successObj) && 
                                successObj is bool success && success)
                            {
                                // Get updated game state if available
                                if (responseData.TryGetValue("gameState", out object stateObj) && 
                                    stateObj is Dictionary<string, object> gameState)
                                {
                                    OnGameStateUpdated?.Invoke(gameState);
                                    onSuccess?.Invoke(gameState);
                                }
                                else
                                {
                                    // If no game state returned, pass the response data
                                    onSuccess?.Invoke(responseData);
                                }
                            }
                            else
                            {
                                // Command failed on server
                                string errorMessage = "Command execution failed on server";
                                if (responseData.TryGetValue("errorMessage", out object errObj) && 
                                    errObj is string errMsg)
                                {
                                    errorMessage = errMsg;
                                }
                                Debug.LogError($"Command {commandName} failed: {errorMessage}");
                                onError?.Invoke(errorMessage);
                            }
                        }
                        else
                        {
                            Debug.LogError($"Command {commandName} returned invalid response");
                            onError?.Invoke("Invalid server response");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error processing command response: {ex.Message}");
                        onError?.Invoke($"Error processing response: {ex.Message}");
                    }
                },
                error => {
                    string errorMessage = error.GenerateErrorReport();
                    Debug.LogError($"Command {commandName} failed: {errorMessage}");
                    onError?.Invoke(errorMessage);
                }
            );
        }

        #endregion
    }
} 
