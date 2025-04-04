# Dokkaebi - Project Status

**Last Updated:** Wednesday, April 2, 2025 at 10:23:46 AM PDT

## Overall Status

**Phase:** Prototype Implementation (In Progress)

**Current Focus:** Critical codebase refactoring and stabilization. Specifically addressing Assembly Definition (.asmdef) structure and resolving related compilation errors to establish a stable foundation before proceeding with further feature implementation outlined in the prototype checklist [cite: Architectural Plan].

## Key Objectives (Prototype)

* Implement core gameplay loop (DTFS, Movement, Basic Abilities, Zones) [cite: Architectural Plan].
* Establish authoritative networking foundation [cite: Architectural Plan].
* Data-driven design using ScriptableObjects [cite: Architectural Plan].
* Support PC and Mobile platforms [cite: Architectural Plan].

## Current Tasks & Blockers

* **Task:** Refactor Assembly Definitions & Dependencies
    * **Status:** In Progress
    * **Details:** Significant refactoring performed previously. Current focus is resolving remaining compilation errors primarily related to dependencies between `Dokkaebi.Grid`, `Dokkaebi.Pathfinding`, `Dokkaebi.Interfaces`, and `Dokkaebi.Utilities`.
    * **Blockers (Current Console Errors):**
        1.  `Assets\Scripts\Dokkaebi\Grid\GridManager.cs(86,26): error CS0311:` Invalid `FindObjectOfType<IPathfinder>()` usage (or related issue from subsequent fix attempts) [cite: User Input].
        2.  `Assets\Scripts\Dokkaebi\Grid\GridNodeUtility.cs(67,50/66): error CS1061:` Incorrect access to `GridPosition` members (likely `x`/`z` vs `X`/`Y`) [cite: User Input, Scripts/Dokkaebi/Interfaces/GridPosition.cs].
        3.  `Assets\Scripts\Dokkaebi\Grid\DokkaebiGridConverter.cs(75,20 / 84,20): error CS0103:` `Utilities` namespace not found, likely due to incorrect `using` directive or missing .asmdef reference [cite: User Input].
    * **Next Steps:** Fix the listed compilation errors by correcting dependency injection/service location in pathfinding scripts, fixing `GridPosition` property access, and resolving namespace/assembly reference issues. Achieve a clean compile.

## Completed Milestones (Recent)

* Initial setup of core systems (partial implementations exist for Grid, Units, Core, UI, etc.) [cite: Codebase Analysis].
* Definition of core interfaces (`Dokkaebi.Interfaces`) for decoupling [cite: Scripts/Dokkaebi/Interfaces/Dokkaebi.Interfaces.asmdef.meta].
* Establishment of data structures (`Dokkaebi.Core.Data`) using ScriptableObjects [cite: Scripts/Dokkaebi/Core/Data/Dokkaebi.Core.Data.asmdef.meta].
* Partial .asmdef refactoring (previous work before current errors) [cite: Handover Artifact].

## Known Issues / Technical Debt

* **Inconsistent Dependency Injection:** Mix of `[SerializeField]`, `FindObjectOfType`, and inefficient loops used across the project [cite: Codebase Analysis]. Needs standardization.
* **Potential Duplicate Code/Classes:** e.g., `AbilityManager` exists in `Dokkaebi.Core` and `Dokkaebi.Abilities`; `GridServices` exists in `Dokkaebi.Core` and `Dokkaebi.Grid` [cite: Scripts/Dokkaebi/Core/AbilityManager.cs, Scripts/Dokkaebi/Abilities/AbilityManager.cs, Scripts/Dokkaebi/Core/GridServices.cs, Scripts/Dokkaebi/Grid/GridServices.cs]. Requires investigation and consolidation.
* **Obsolete Code:** Some classes marked as deprecated (e.g., `DokkaebiGridConverter` [cite: Scripts/Dokkaebi/Grid/DokkaebiGridConverter.cs], `ZoneInstance_DEPRECATED` [cite: Scripts/Dokkaebi/Grid/ZoneInstance_DEPRECATED.cs]). Need cleanup once fully migrated.
* **Missing Implementations:** Many systems outlined in the Architectural Plan have basic structures or stubs but require full implementation (e.g., `MovementManager`, `AuraManager`, full networking logic).

*(Add other known major bugs or impediments here)*

## Next Steps (Post-Refactor)

* Proceed with implementing features outlined in the prototype checklist (`tasks.md`) [cite: Architectural Plan].
* Select and integrate the chosen networking library [cite: Architectural Plan].
* Develop authoritative server logic [cite: Architectural Plan].
* Write unit and integration tests for implemented features [cite: Architectural Plan].