# Turn System Refactoring Guide

## Overview

The Dokkaebi turn system has been refactored to use the more robust `DokkaebiTurnSystemCore` as the primary implementation. The older `TurnManager` has been converted to a compatibility adapter that delegates to `DokkaebiTurnSystemCore` while maintaining its public API.

This document outlines the steps necessary to complete the migration from `TurnManager` to `DokkaebiTurnSystemCore`.

## Current Status

- `DokkaebiTurnSystemCore` is now the primary implementation of the turn system
- `TurnManager` is a compatibility adapter that delegates to `DokkaebiTurnSystemCore`
- Key systems (PlayerActionManager, GameStateManager) have been updated to use both systems with preference for `DokkaebiTurnSystemCore`

## Migration Guide

### Step 1: Update References in MonoBehaviour Components

Update any component that references `TurnManager` to use `DokkaebiTurnSystemCore` instead:

```csharp
// Before
[SerializeField] private TurnManager turnManager;

private void Awake()
{
    if (turnManager == null) turnManager = FindObjectOfType<TurnManager>();
}

// After
[SerializeField] private DokkaebiTurnSystemCore turnSystem;

private void Awake()
{
    if (turnSystem == null) turnSystem = FindObjectOfType<DokkaebiTurnSystemCore>();
}
```

### Step 2: Update API Usage

`TurnManager` and `DokkaebiTurnSystemCore` have different APIs. Here's how to migrate common operations:

#### Phase Changes

```csharp
// Before
turnManager.ChangePhase(GamePhase.Movement);

// After
turnSystem.ForceTransitionTo(TurnPhase.MovementPhase);
```

#### Get Current Phase

```csharp
// Before
GamePhase currentPhase = turnManager.CurrentPhase;

// After
TurnPhase currentPhase = turnSystem.GetCurrentPhase();
```

#### Check If Player Turn

```csharp
// Before
bool isPlayerTurn = turnManager.IsPlayerTurn;

// After
bool isPlayerTurn = turnSystem.IsPlayerTurn();
```

#### Get Current Turn

```csharp
// Before
int currentTurn = turnManager.CurrentTurn;

// After
int currentTurn = turnSystem.GetCurrentTurn();
```

#### End Current Phase

```csharp
// Before
turnManager.EndCurrentPhase();

// After
turnSystem.NextPhase();
```

### Step 3: Update Event Handlers

Event handlers need to be updated to match the new event signatures:

```csharp
// Before
turnManager.OnPhaseChanged += (GamePhase phase) => { /* ... */ };

// After
turnSystem.OnPhaseChanged += (TurnPhase phase) => { /* ... */ };
```

### Step 4: Phase Mapping

`TurnPhase` in `DokkaebiTurnSystemCore` has more granular phases than `GamePhase` in `TurnManager`:

| TurnManager GamePhase | DokkaebiTurnSystemCore TurnPhase |
|-----------------------|----------------------------------|
| GamePhase.None        | TurnPhase.Opening                |
| GamePhase.Movement    | TurnPhase.MovementPhase          |
| GamePhase.Aura        | TurnPhase.AuraPhase1A / TurnPhase.AuraPhase1B / TurnPhase.AuraPhase2A / TurnPhase.AuraPhase2B |
| GamePhase.Resolution  | (No direct mapping)              |
| GamePhase.End         | (No direct mapping)              |

Use the `TurnPhaseHelper` static class to check phase types:

```csharp
// Check if a movement phase
bool isMovementPhase = TurnPhaseHelper.IsMovementPhase(currentPhase);

// Check if an aura phase
bool isAuraPhase = TurnPhaseHelper.IsAuraPhase(currentPhase);
```

### Step 5: Remove TurnManager

Once all dependencies have been migrated to use `DokkaebiTurnSystemCore`, the `TurnManager` compatibility adapter can be removed.

## Key Differences Between Systems

- `DokkaebiTurnSystemCore` uses a state machine via `TurnStateContext` for turn flow
- Aura phases are more granular in `DokkaebiTurnSystemCore` (separate phases for player 1 and player 2)
- `DokkaebiTurnSystemCore` has better unit tracking and validation
- `DokkaebiTurnSystemCore` has better handling of phase transitions and locks
- `DokkaebiTurnSystemCore` uses an observer pattern for updates via `DokkaebiUpdateManager`

## Questions?

If you have questions about the migration, contact the core gameplay team. 