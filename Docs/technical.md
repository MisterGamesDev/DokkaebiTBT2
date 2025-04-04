# Dokkaebi - Technical Specifications & Patterns

## 1. Core Technologies

* **Game Engine:** Unity 6 (Version 6000.0.36f1)
* **Primary Language:** C# (latest version supported by Unity 6)
* **Pathfinding:** A* Pathfinding Project ( Aron Granberg)
* **Input:** Unity Input System Package
* **UI:** Unity UI (Canvas/TextMeshPro) / UI Toolkit (Investigate suitability) [cite: Architectural Plan].
* **Networking (Planned):** Authoritative server using PlayFab + Azure Functions. Specific transport library (e.g., Netcode for GameObjects) to be determined [cite: Architectural Plan].
* **Version Control:** Git

## 2. Target Platforms

* **Primary:** PC (Steam)
* **Secondary:** Mobile (iOS/Android) - Crossplay planned [cite: GDD, Architectural Plan]. *(Design and implementation should consider mobile performance constraints)* [cite: Architectural Plan].

## 3. Coding Standards & Style

* **Naming Conventions:** Standard C# conventions:
    * `PascalCase` for classes, structs, enums, methods, properties, events.
    * `camelCase` for local variables, method parameters.
    * Private fields use `camelCase` (consistency check needed for `_` prefix usage observed in some files [cite: Scripts/Dokkaebi/Pathfinding/DokkaebiMovementHandler.cs] vs none in others [cite: Scripts/Dokkaebi/Units/DokkaebiUnit.cs]).
* **`using` Directives:** Placed outside namespace declarations.
* **Documentation:** Public APIs and complex internal logic documented using XML documentation comments (`<summary>`, `<param>`, etc.).
* **Readability:** Emphasis on clear, concise, well-formatted code. Small, focused methods and classes preferred.
* **Style Enforcement:** Adhere to project-defined style via `.editorconfig` (if present) or team conventions. Use IDE formatting tools.

## 4. Architectural Patterns & Principles

* **Modularity:** Code organized into distinct systems/assemblies (.asmdef) with clear responsibilities (e.g., `GridManager`, `UnitManager`, `TurnSystemCore`) [cite: Architectural Plan].
* **Separation of Concerns:** Logic, Presentation, Input, and Networking are kept distinct [cite: Architectural Plan].
* **Data-Driven Design:** `ScriptableObjects` used extensively for defining game data (Units, Abilities, Zones, Origins, Callings) decoupling data from code [cite: Architectural Plan, Scripts/Dokkaebi/Core/Data/UnitDefinitionData.cs]. `DataManager` serves as a registry [cite: Scripts/Dokkaebi/Core/Data/DataManager.cs].
* **Event-Driven Communication:** C# `event` / `Action` delegates used for loose coupling between systems (e.g., `TurnSystemCore.OnPhaseChanged`, `Unit.OnDamaged`) [cite: Architectural Plan, Scripts/Dokkaebi/Core/DokkaebiTurnSystemCore.cs, Scripts/Dokkaebi/Units/DokkaebiUnit.cs].
* **State Pattern:** Used in `DokkaebiTurnSystemCore` via `TurnStateContext` and `ITurnPhaseState` implementations to manage turn flow [cite: Architectural Plan, Scripts/Dokkaebi/Core/TurnStateContext.cs, Scripts/Dokkaebi/Core/TurnStates/ITurnPhaseState.cs].
* **Command Pattern:** Used for player actions (`PlayerActionManager`, `ICommand` implementations) to encapsulate requests and facilitate networking [cite: Architectural Plan, Scripts/Dokkaebi/Core/PlayerActionManager.cs, Scripts/Dokkaebi/Core/Networking/Commands/ICommand.cs].
* **Interface-Based Decoupling:** Interfaces (`IDokkaebiUnit`, `IGridSystem`, `IPathfinder`, `IPathfindingGridInfo`, etc.) defined in `Dokkaebi.Interfaces` assembly are used extensively to reduce direct dependencies between modules [cite: Architectural Plan, Scripts/Dokkaebi/Interfaces/Dokkaebi.Interfaces.asmdef.meta].
* **Dependency Injection / Service Location:** Currently inconsistent. Mix of `[SerializeField]` for Inspector assignment, `GetComponent`, `FindObjectOfType` (often as fallback), and problematic `FindObjectsByType<MonoBehaviour>` loops [cite: Scripts/Dokkaebi/Pathfinding/DokkaebiMovementHandler.cs, Scripts/Dokkaebi/Core/AbilityManager.cs, Scripts/Dokkaebi/Pathfinding/PathfindingNodeProvider.cs]. *Recommendation: Standardize on `[SerializeField]` where practical, or implement a simple Service Locator for global managers.*
* **Update Management:** `DokkaebiUpdateManager` uses an observer pattern to manage `Update` calls centrally, aiming to improve performance [cite: Architectural Plan, Scripts/Dokkaebi/Core/DokkaebiUpdateManager.cs].
* **Singleton Pattern:** Used for some managers (e.g., `GridManager`, `ZoneManager`, `DataManager`) for global access [cite: Scripts/Dokkaebi/Grid/GridManager.cs, Scripts/Dokkaebi/Zones/ZoneManager.cs, Scripts/Dokkaebi/Core/Data/DataManager.cs]. Use with caution; prefer dependency injection where possible.
* **Object Pooling:** Recommended for frequent instantiations like VFX, SFX, and potentially zone visuals to optimize performance, especially on mobile [cite: Architectural Plan]. (Currently not explicitly implemented in provided code).
* **Utility Classes:** Static utility classes used for common functions (e.g., `GridPositionConverter` [cite: Scripts/Dokkaebi/Utilities/GridPositionConverter.cs], `SmartLogger` [cite: Scripts/Dokkaebi/Utilities/SmartLogger.cs]).

## 5. Coordinate System

* **Design:** 1-based (X, Y) with (1, 1) at bottom-left [cite: Architectural Plan].
* **Implementation (Unity):** Mapped to 0-based `Vector2Int` (X, Y) corresponding to grid indices [0..9, 0..9] and `Vector3` (X, Z) for world positions [cite: Architectural Plan].
* **Translation:** `GridManager` and utility converters (`Dokkaebi.Interfaces.GridConverter`, `Dokkaebi.Utilities.GridPositionConverter`) handle translations between world positions, `Vector2Int`, and the `Dokkaebi.Interfaces.GridPosition` struct (which uses x, z fields) [cite: Architectural Plan, Scripts/Dokkaebi/Grid/GridManager.cs, Scripts/Dokkaebi/Interfaces/GridConverter.cs, Scripts/Dokkaebi/Utilities/GridPositionConverter.cs, Scripts/Dokkaebi/Interfaces/GridPosition.cs]. *Note: Consistency check needed between different converter classes.*

## 6. Testing Strategy

* **Unit Tests:** Use Unity Test Framework for pure logic components (combat calculations, state transitions, validation rules, utilities) [cite: Architectural Plan].
* **Integration Tests:** Use Unity Test Framework for testing interactions between multiple systems (e.g., Input -> Action -> Turn -> Unit execution) [cite: Architectural Plan].
* **Manual Testing:** Essential for UI, gameplay feel, and validating visual/audio feedback in the Unity Editor Play mode.

## 7. Performance Considerations

* **Mobile First:** Design with mobile constraints in mind (CPU, GPU, memory) [cite: Architectural Plan].
* **Update Loop:** Minimize work done in `Update`. Use `DokkaebiUpdateManager` where applicable [cite: Scripts/Dokkaebi/Core/DokkaebiUpdateManager.cs].
* **Instantiations:** Use object pooling for frequently created/destroyed objects (VFX, projectiles, UI elements) [cite: Architectural Plan].
* **Pathfinding:** Ensure pathfinding queries via `GridManager`/`DokkaebiPathfinder` are performant, especially considering dynamic costs from zones [cite: Architectural Plan].
* **UI:** Optimize UI updates to avoid excessive draw calls or layout recalculations [cite: Architectural Plan]. Profile UI performance.
* **Logging:** Use `SmartLogger` and disable unnecessary log categories in builds [cite: Scripts/Dokkaebi/Utilities/SmartLogger.cs].