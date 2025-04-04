# Dokkaebi - Development Tasks

## Current Sprint / Focus

**Goal:** Stabilize codebase by resolving Assembly Definition (.asmdef) related compilation errors and refactoring dependencies, particularly between Grid and Pathfinding.

**Tasks:**

1.  **[In Progress] Refactor Assembly Definitions & Dependencies**
    * **Goal:** Achieve clean compilation with correct module boundaries enforced by .asmdef files. Resolve cyclic dependencies and missing references.
    * **Sub-Task:** Refactor Dependency Acquisition in Pathfinding Scripts (Task 32 from handover)
        * **Goal:** Resolve compilation errors related to how `PathfindingNodeProvider` and `DokkaebiPathfinder` acquire references to `IGridSystem` and `IPathfindingGridInfo`.
        * **Current Blockers (Console Errors):**
            * `Assets\Scripts\Dokkaebi\Grid\GridManager.cs(86,26): error CS0311: The type 'Dokkaebi.Interfaces.IPathfinder' cannot be used as type parameter 'T' in the generic type or method 'Object.FindObjectOfType<T>()'. There is no implicit reference conversion from 'Dokkaebi.Interfaces.IPathfinder' to 'UnityEngine.Object'.` *(Note: This error might stem from a previous attempt; current code uses a different method but still needs fixing)*.
            * `Assets\Scripts\Dokkaebi\Grid\GridNodeUtility.cs(67,50): error CS1061: 'GridPosition' does not contain a definition for 'X' and no accessible extension method 'X' accepting a first argument of type 'GridPosition' could be found (are you missing a using directive or an assembly reference?)`
            * `Assets\Scripts\Dokkaebi\Grid\GridNodeUtility.cs(67,66): error CS1061: 'GridPosition' does not contain a definition for 'Y' and no accessible extension method 'Y' accepting a first argument of type 'GridPosition' could be found (are you missing a using directive or an assembly reference?)`
            * `Assets\Scripts\Dokkaebi\Grid\DokkaebiGridConverter.cs(75,20): error CS0103: The name 'Utilities' does not exist in the current context`
            * `Assets\Scripts\Dokkaebi\Grid\DokkaebiGridConverter.cs(84,20): error CS0103: The name 'Utilities' does not exist in the current context`
        * **Action:** Implement a robust dependency acquisition method (e.g., `FindObjectOfType<GridManager>()` cast to interface, or `[SerializeField]`) in the affected pathfinding scripts [cite: Scripts/Dokkaebi/Pathfinding/PathfindingNodeProvider.cs, Scripts/Dokkaebi/Pathfinding/DokkaebiPathfinder.cs]. Fix related `GridPosition` access and namespace errors.
    * **Sub-Task:** Verify all `.asmdef` file references are correct and minimal.
    * **Acceptance:** Project compiles without errors.

## Prototype Implementation Tasks (Based on Arch Plan Checklist)

*(These tasks represent the broader goals for building the prototype, listed here for context. Status/Priority/Assignment TBD)*

* [ ] Set up project structure & initial .asmdef files.
* [ ] Implement core ScriptableObject data structures (`OriginData`, `CallingData`, `AbilityData`, `ZoneData`, `StatusEffectData`, `UnitSpawnData`) [cite: Architectural Plan].
* [ ] Build `GridManager` (0-based coords, tile data, basic pathfinding integration) [cite: Architectural Plan].
* [ ] Implement `DataManager` [cite: Architectural Plan].
* [ ] Create `DokkaebiUnit` prefab/component (state tracking, interface implementation) [cite: Architectural Plan].
* [ ] Implement `UnitManager` (fixed spawns based on `UnitSpawnData`) [cite: Architectural Plan].
* [ ] Build `DokkaebiTurnSystemCore` state machine [cite: Architectural Plan].
* [ ] Set up Input System package & `InputManager` [cite: Architectural Plan].
* [ ] Implement `PlayerActionManager` (Command Pattern, validation) [cite: Architectural Plan].
* [ ] Select and integrate Networking Library (basic connection, command send/receive) [cite: Architectural Plan].
* [ ] Build authoritative server logic (stubbed or basic implementation for prototype) [cite: Architectural Plan].
* [ ] Implement `MovementManager` logic (simultaneous resolution, conflict handling) [cite: Architectural Plan].
* [ ] Implement `AuraManager` and MP gain logic [cite: Architectural Plan].
* [ ] Build `AbilityManager` (execute prototype abilities, costs, cooldowns, repositioning) [cite: Architectural Plan].
* [ ] Develop `ZoneManager` (creation, effects, merging, shifting, resonance, void space) [cite: Architectural Plan].
* [ ] Create basic `UIManager` (HUD, unit info, ability buttons, turn display) [cite: Architectural Plan].
* [ ] Implement C# event connections between systems [cite: Architectural Plan].
* [ ] Write Unit Tests for critical logic [cite: Architectural Plan].
* [ ] Implement basic network error handling/logging [cite: Architectural Plan].
* [ ] Setup Version Control (Git) repository.

## Backlog / Future

* Implement remaining non-prototype features (Overwatch, Calling Absorption, Dynamic Terrain, advanced abilities, etc.) [cite: Architectural Plan].
* Full networking implementation and optimization.
* AI for non-player units.
* Advanced UI/UX features.
* Art asset integration.
* Performance optimization passes.
* Refactor code using DokkaebiGridConverter