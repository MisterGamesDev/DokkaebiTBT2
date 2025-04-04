# Dokkaebi - PlayFab Integration Guide

This document outlines the steps to set up the authoritative PlayFab server backend for Dokkaebi.

## Overview

Dokkaebi uses PlayFab's authoritative server model with Azure Functions to validate and execute all gameplay actions. The client sends Commands via the NetworkingManager, which are then validated and executed on the server. The server updates the game state and returns it to all connected clients.

## Setup Steps

### 1. Create a PlayFab Account & Title

1. Sign up for a PlayFab account at [playfab.com](https://playfab.com)
2. Create a new title for your game (e.g., "Dokkaebi")
3. Note your Title ID, which will be needed in the Unity project

### 2. Install PlayFab SDK in Unity

1. Download the PlayFab SDK for Unity:
   - Via the PlayFab Editor Extensions (recommended)
   - Or directly from the [PlayFab GitHub repository](https://github.com/PlayFab/UnitySDK)

2. Import the SDK into your Unity project
3. Configure your Title ID in the Unity project:
   - In the PlayFab Editor Extensions window
   - Or directly in the `NetworkingManager` component

### 3. Azure Functions Setup

#### 3.1 Create an Azure Functions App

1. Sign in to the [Azure Portal](https://portal.azure.com)
2. Create a new Function App:
   - Select "Create a resource" > "Compute" > "Function App"
   - Choose a name, subscription, resource group
   - Select ".NET Core" as the runtime stack
   - Choose the region closest to your players

#### 3.2 Connect Azure Functions to PlayFab

1. In PlayFab Game Manager, go to "Automation" > "Azure Functions"
2. Click "Link Azure Function App"
3. Follow the prompts to link your Azure Function App

#### 3.3 Implement Server-Side Functions

Create the following Azure Functions for each game action:

- `ExecuteMoveCommand` - Validates and executes unit movement
- `ExecuteAbilityCommand` - Validates and executes ability usage
- `ExecuteRepositionCommand` - Validates and executes tactical repositioning
- `ExecuteEndTurnCommand` - Processes turn end logic
- `ExecuteActivateAuraCommand` - Processes aura activation
- `GetGameState` - Retrieves current game state

Each function should follow this general structure:

```csharp
// Example Azure Function for ExecuteMoveCommand
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab.Samples;

public static class MoveCommandFunction
{
    [FunctionName("ExecuteMoveCommand")]
    public static async Task<dynamic> ExecuteMoveCommand(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        ILogger log)
    {
        // Get the context objects
        var context = await FunctionContext<dynamic>.Create(req);
        
        // Extract arguments from the request body
        dynamic args = context.FunctionArgument;
        string matchId = args.matchId;
        dynamic commandData = args.commandData;
        string unitId = commandData.unitId;
        int targetX = (int)commandData.targetX;
        int targetY = (int)commandData.targetY;
        
        // Load current game state from Shared Group Data
        var gameState = await LoadGameState(context, matchId);
        
        // Validate command
        string validationError = ValidateMoveCommand(gameState, unitId, targetX, targetY);
        if (!string.IsNullOrEmpty(validationError))
        {
            return new {
                success = false,
                errorMessage = validationError
            };
        }
        
        // Execute movement logic
        ExecuteMove(gameState, unitId, targetX, targetY);
        
        // Save updated game state
        await SaveGameState(context, matchId, gameState);
        
        // Return success with updated game state
        return new {
            success = true,
            gameState = gameState
        };
    }
    
    // Helper methods for validation, execution, and state management
    // ...
}
```

### 4. Game State Management

#### 4.1 Configure Shared Group Data for Match State

1. Create a PlayFab Group for each match at game start
2. Store game state in Shared Group Data
3. Restrict client write access to Shared Group Data (server-only writes)

Example code structure for state management:

```csharp
// Load game state from Shared Group Data
private static async Task<Dictionary<string, object>> LoadGameState(FunctionContext<dynamic> context, string matchId)
{
    try
    {
        var result = await context.PlayFabAPIWrapper.GetSharedGroupDataAsync(new PlayFab.ServerModels.GetSharedGroupDataRequest
        {
            SharedGroupId = matchId,
            Keys = new List<string> { "gameState" }
        });
        
        if (result.Data.ContainsKey("gameState") && !string.IsNullOrEmpty(result.Data["gameState"].Value))
        {
            return JsonConvert.DeserializeObject<Dictionary<string, object>>(result.Data["gameState"].Value);
        }
    }
    catch (Exception ex)
    {
        context.Logger.LogError($"Error loading game state: {ex.Message}");
    }
    
    // Return empty state if not found
    return new Dictionary<string, object>();
}

// Save game state to Shared Group Data
private static async Task SaveGameState(FunctionContext<dynamic> context, string matchId, Dictionary<string, object> gameState)
{
    try
    {
        await context.PlayFabAPIWrapper.UpdateSharedGroupDataAsync(new PlayFab.ServerModels.UpdateSharedGroupDataRequest
        {
            SharedGroupId = matchId,
            Data = new Dictionary<string, string>
            {
                { "gameState", JsonConvert.SerializeObject(gameState) }
            }
        });
    }
    catch (Exception ex)
    {
        context.Logger.LogError($"Error saving game state: {ex.Message}");
    }
}
```

### 5. Testing Your Integration

1. Use the PlayFab Explorer in Unity to test API calls
2. Check Azure Function logs for debugging
3. Use PlayFab's Shared Group Data Explorer to view stored game state

## Match Flow Integration

### Starting a Match

1. Client creates a match and shared group via NetworkingManager
2. Server initializes game state in Shared Group Data
3. Clients receive initial state

### During Gameplay

1. Client creates a Command object via PlayerActionManager
2. NetworkingManager sends Command to relevant Azure Function
3. Server validates, executes, and updates game state
4. Updated state is returned to clients
5. GameStateManager applies changes to local game objects

### Match Completion

1. Server detects win condition
2. Updates game state with match result
3. Clients transition to results screen

## Additional Resources

- [PlayFab Documentation](https://docs.microsoft.com/en-us/gaming/playfab/)
- [Azure Functions Documentation](https://docs.microsoft.com/en-us/azure/azure-functions/)
- [PlayFab Unity SDK Reference](https://api.playfab.com/sdks/unity) 