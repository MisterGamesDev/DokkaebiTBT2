# Dokkaebi - System Architecture

## 1. Overview

Dokkaebi employs a modular architecture built in Unity using C#. Key principles include data-driven design (ScriptableObjects), event-driven communication, separation of concerns (Logic, Presentation, Input, Networking), and an authoritative server model for networking [cite: Architectural Plan]. The codebase is organized into namespaces and assemblies (.asmdef files) to manage dependencies [cite: Scripts/Dokkaebi/Grid/Dokkaebi.Grid.asmdef.meta, Scripts/Dokkaebi/Pathfinding/Dokkaebi.Pathfinding.asmdef.meta, Scripts/Dokkaebi/Interfaces/Dokkaebi.Interfaces.asmdef.meta].

## 2. Core Modules & Responsibilities

*(Based heavily on the Architectural Plan and codebase structure)*

* **`Dokkaebi.Core`**: Contains central managers coordinating the game flow.
    * `MatchManager`: Oversees match lifecycle, timing, win conditions, and manager coordination [cite: Architectural Plan]. (Not fully implemented in provided code).
    * `DokkaebiTurnSystemCore`: Manages the DTFS turn flow using a State Pattern (`TurnStateContext`, `ITurnPhaseState`) [cite: Architectural Plan, Scripts/Dokkaebi/Core/DokkaebiTurnSystemCore.cs, Scripts/Dokkaebi/Core/TurnStateContext.cs]. Dictates allowed actions per phase.
    * `UnitManager`: Handles unit lifecycle (spawning via `UnitSpawnData` [cite: Scripts/Dokkaebi/Core/Data/UnitSpawnData.cs]), registry, and querying [cite: Architectural Plan, Scripts/Dokkaebi/Core/UnitManager.cs].
    * `PlayerActionManager`: Receives input intents, validates actions, creates Commands (Move, Ability, Reposition) [cite: Architectural Plan, Scripts/Dokkaebi/Core/PlayerActionManager.cs, Scripts/Dokkaebi/Core/Networking/Commands/CommandBase.cs].
    * `MovementManager`: (Planned) Resolves simultaneous movement phase based on server commands, handles conflicts and pathing requests [cite: Architectural Plan].
    * `AbilityManager` / `CombatManager`: Executes validated ability/reposition commands, applies effects, manages costs/cooldowns [cite: Architectural Plan, Scripts/Dokkaebi/Core/AbilityManager.cs, Scripts/Dokkaebi/Abilities/AbilityManager.cs]. *(Note: Duplicate AbilityManager found)*
    * `AuraManager`: (Planned) Tracks player Aura resources [cite: Architectural Plan].
    * `DokkaebiUpdateManager`: Central manager for optimized `Update` calls using an observer pattern [cite: Architectural Plan, Scripts/Dokkaebi/Core/DokkaebiUpdateManager.cs].
* **`Dokkaebi.Core.Data`**: Defines ScriptableObject structures (`UnitDefinitionData`, `AbilityData`, `ZoneData`, etc.) for game configuration [cite: Architectural Plan, Scripts/Dokkaebi/Core/Data/UnitDefinitionData.cs, Scripts/Dokkaebi/Core/Data/AbilityData.cs, Scripts/Dokkaebi/Core/Data/ZoneData.cs].
    * `DataManager`: Loads and provides access to `ScriptableObject` data assets [cite: Architectural Plan, Scripts/Dokkaebi/Core/Data/DataManager.cs].
* **`Dokkaebi.Core.Networking`**: Manages communication with the authoritative server (PlayFab/Azure Functions planned). Includes Command pattern implementation and game state synchronization structures (`GameStateData`) [cite: Architectural Plan, Scripts/Dokkaebi/Core/Networking/NetworkingManager.cs, Scripts/Dokkaebi/Core/Networking/Commands/CommandBase.cs, Scripts/Dokkaebi/Core/Networking/GameStateData.cs].
    * `NetworkingManager`: Handles connection, authentication, command sending, and state reception [cite: Scripts/Dokkaebi/Core/Networking/NetworkingManager.cs].
    * `GameStateManager`: Applies received server state to local managers [cite: Scripts/Dokkaebi/Core/Networking/GameStateManager.cs].
* **`Dokkaebi.Grid`**: Manages the logical 10x10 grid.
    * `GridManager`: Stores tile data (occupancy, zones), performs coordinate conversions, provides pathfinding queries via `IPathfindingGridInfo`, handles visualization [cite: Architectural Plan, Scripts/Dokkaebi/Grid/GridManager.cs]. Implements `IGridSystem` and `IPathfindingGridInfo` [cite: Scripts/Dokkaebi/Grid/GridManager.cs].
    * `GridNodeUtility`: Provides grid-based node lookups using `INodeProvider` to decouple from specific pathfinding implementation [cite: Scripts/Dokkaebi/Grid/GridNodeUtility.cs].
* **`Dokkaebi.Units`**: Contains the unit implementation.
    * `DokkaebiUnit`: Represents unit state (HP, MP, position, status, cooldowns) and behaviour. Implements `IDokkaebiUnit` [cite: Architectural Plan, Scripts/Dokkaebi/Units/DokkaebiUnit.cs, Scripts/Dokkaebi/Interfaces/IDokkaebiUnit.cs]. Interacts with `DokkaebiMovementHandler`.
* **`Dokkaebi.Pathfinding`**: Integrates the A* Pathfinding Project.
    * `DokkaebiMovementHandler`: Attached to units, handles path requests (via `Seeker`) and path following [cite: Scripts/Dokkaebi/Pathfinding/DokkaebiMovementHandler.cs].
    * `DokkaebiPathfinder`: Implements `IPathfinder` using A* logic, interacting with `IGridSystem` and `IPathfindingGridInfo` [cite: Scripts/Dokkaebi/Pathfinding/DokkaebiPathfinder.cs, Scripts/Dokkaebi/Interfaces/IPathfinder.cs].
    * `PathfindingNodeProvider`: Implements `INodeProvider` using A* to provide `IGraphNode` access [cite: Scripts/Dokkaebi/Pathfinding/PathfindingNodeProvider.cs, Scripts/Dokkaebi/Interfaces/INodeProvider.cs].
    * `GraphNodeAdapter`: Adapts A*'s `GraphNode` to the `IGraphNode` interface [cite: Scripts/Dokkaebi/Pathfinding/GraphNodeAdapter.cs, Scripts/Dokkaebi/Interfaces/IGraphNode.cs].
* **`Dokkaebi.Zones`**: Manages battlefield zones.
    * `ZoneManager`: Handles `ZoneInstance` lifecycle, merging, resonance, unstable resonance, void space, and applies effects [cite: Architectural Plan, Scripts/Dokkaebi/Zones/ZoneManager.cs].
    * `ZoneInstance`: Represents an active zone, holding runtime data and effects [cite: Scripts/Dokkaebi/Zones/ZoneInstance.cs].
* **`Dokkaebi.Abilities`**: Contains logic related to ability execution *(Note: Some logic currently duplicated in `Dokkaebi.Core.AbilityManager`)*.
* **`Dokkaebi.UI`**: Handles presentation and user interaction feedback.
    * `UIManager`: Coordinates various UI panels [cite: Scripts/UI/UIManager.cs].
    * Specific panels for Unit Info, Ability Selection, Turn Phase, Player Resources, Game Over [cite: Scripts/UI/UnitInfoPanel.cs, Scripts/UI/AbilitySelectionUI.cs, Scripts/UI/TurnPhaseUI.cs, Scripts/UI/PlayerResourceUI.cs, Scripts/UI/GameOverUI.cs].
* **`Dokkaebi.Input`**: Manages player input.
    * `InputManager`: Uses Unity's Input System, translates input to actions sent to `PlayerActionManager` [cite: Architectural Plan, Scripts/Dokkaebi/Input/InputManager.cs].
* **`Dokkaebi.Common`**: Shared enums and simple data structures used across multiple assemblies to avoid circular dependencies [cite: Scripts/Dokkaebi/Common/README.md, Scripts/Dokkaebi/Common/CommonEnums.cs]. *(Note: Some interfaces currently reside here but might be better in `Dokkaebi.Interfaces`)*.
* **`Dokkaebi.Interfaces`**: Defines interfaces (`IGridSystem`, `IDokkaebiUnit`, `IPathfinder`, `IPathfindingGridInfo`, `INodeProvider`, etc.) to decouple modules [cite: Scripts/Dokkaebi/Interfaces/IGridSystem.cs, Scripts/Dokkaebi/Interfaces/IDokkaebiUnit.cs, Scripts/Dokkaebi/Interfaces/IPathfinder.cs, Scripts/Dokkaebi/Interfaces/IPathfindingGridInfo.cs, Scripts/Dokkaebi/Interfaces/INodeProvider.cs]. Includes shared `GridPosition` struct [cite: Scripts/Dokkaebi/Interfaces/GridPosition.cs].
* **`Dokkaebi.Utilities`**: General utility classes like `SmartLogger` [cite: Scripts/Dokkaebi/Utilities/SmartLogger.cs] and `GridPositionConverter` [cite: Scripts/Dokkaebi/Utilities/GridPositionConverter.cs].

## 3. Data Flow (Example: Move Action)

1.  **Input:** `InputManager` detects click on grid via Unity Input System.
2.  **Intent:** `InputManager` translates click to grid coordinate, sends intent to `PlayerActionManager`.
3.  **Validation:** `PlayerActionManager` checks `DokkaebiTurnSystemCore` (is Movement Phase?), selected `DokkaebiUnit` (can move?), `GridManager` (is target reachable/walkable within range?).
4.  **Command Creation:** Valid action -> `PlayerActionManager` creates `MoveCommand` [cite: Scripts/Dokkaebi/Core/Networking/Commands/MoveCommand.cs].
5.  **Network Send:** `PlayerActionManager` sends command via `NetworkingManager`.
6.  **Server:** Authoritative server receives command, re-validates, calculates resolution (including conflicts via `MovementManager` logic), updates authoritative game state.
7.  **Network Receive:** `NetworkingManager` receives updated game state/command result.
8.  **State Update:** `GameStateManager` parses state, updates relevant local managers (`UnitManager`, `GridManager`, `TurnManager`).
9.  **Execution (Visual):** `MovementManager` (or `DokkaebiMovementHandler` triggered by state change) visually moves the unit along the resolved path.
10. **UI Update:** `UIManager` listens to events (e.g., `OnMoved`, `OnPhaseChanged`) and updates display.

## 4. Assembly Definitions (.asmdef)

The project utilizes Assembly Definitions to structure the codebase and manage dependencies, aiming to improve compilation times and enforce architectural boundaries [cite: Architectural Plan]. Key assemblies observed/inferred include:

* `Dokkaebi.Interfaces` (Lowest level, defining contracts)
* `Dokkaebi.Common` (Shared enums/structs - potential overlap with Interfaces)
* `Dokkaebi.Utilities` (General helpers)
* `Dokkaebi.Core.Data` (ScriptableObject definitions)
* `Dokkaebi.Grid` (Depends on Interfaces, maybe Common/Utilities)
* `Dokkaebi.Pathfinding` (Depends on Interfaces, Common, Utilities, AstarPathfindingProject)
* `Dokkaebi.Units` (Depends on Interfaces, Core.Data, Grid, Pathfinding, Common, Utilities)
* `Dokkaebi.Zones` (Depends on Interfaces, Core.Data, Grid, Units, Common, Utilities)
* `Dokkaebi.Abilities` (Depends on Interfaces, Core.Data, Grid, Units, Zones, Common, Utilities)
* `Dokkaebi.Core` (Depends on most other assemblies, coordinates managers)
* `Dokkaebi.Input` (Depends on Core, Grid, Units, Interfaces)
* `Dokkaebi.UI` (Depends on Core, Grid, Units, Input, Interfaces)
* `Dokkaebi.Networking` (Depends on Core, Interfaces)
* `Dokkaebi.Main` (Likely top-level assembly, depends on Core, UI, Input etc.)

*(Note: Exact dependencies need verification by inspecting the `.asmdef` files themselves. The current refactoring focuses on resolving issues within this structure).*

## 5. Networking Architecture

* **Model:** Authoritative Server [cite: Architectural Plan]. The server (planned as Azure Functions interacting with PlayFab) holds the true state.
* **Client Role:** Sends validated player actions as Commands (`MoveCommand`, `AbilityCommand`, etc.) [cite: Scripts/Dokkaebi/Core/Networking/Commands/CommandBase.cs]. Receives state updates from the server. Renders the game state.
* **Server Role:** Validates commands, resolves actions (especially simultaneous movement), updates the game state stored potentially in PlayFab Shared Group Data, and broadcasts updates [cite: Architectural Plan].
* **State Sync:** Event-Based Updates or Phase Snapshots planned for prototype. Full state or delta compression might be needed later [cite: Architectural Plan]. Key synchronized data includes unit positions, HP, MP, status, cooldowns, Aura, zones, turn info [cite: Architectural Plan, Scripts/Dokkaebi/Core/Networking/GameStateData.cs].