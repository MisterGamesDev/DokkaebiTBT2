# Dokkaebi.Common Namespace

This namespace contains shared interfaces, data structures, and enums that are referenced across multiple assemblies. It's designed to prevent circular dependencies between modules.

## Organization

### Interfaces
- **IAbility.cs** - Interfaces for abilities and ability instances
- **IGridSystem.cs** - Interface for grid management
- **IPathfinding.cs** - Interface for pathfinding services
- **ITurnSystem.cs** - Interface for turn management
- **IUnitSystem.cs** - Interface for unit system functions
- **IZoneSystem.cs** - Interface for zone management
- **InterfacesUnit.cs** - Interfaces for units and status effects

### Data Structures
- **GridPosition.cs** - Structure for representing grid coordinates
- **Direction.cs** - Enum and utilities for grid directions

### Enums
- **CommonEnums.cs** - Shared enums used across the project

## Usage Guidelines

1. **Avoid Circular References**: Keep the Common namespace free of dependencies on other namespaces.

2. **Interface Design**: When adding a new interface:
   - Focus on the minimum contract needed for cross-module communication
   - Avoid exposing implementation details
   - Document the purpose clearly in XML comments

3. **Enum Consolidation**: Keep all shared enums in CommonEnums.cs to avoid duplication.

4. **Event-based Communication**: Use C# events/delegates for communication between systems.

## Recent Changes

- Renamed conflicting interfaces to avoid namespace collisions:
  - `IStatusEffect` in InterfacesUnit.cs renamed to `IStatusEffectInstance` (represents a specific instance of a status effect on a unit)
  - `IDokkaebiUnit` in InterfacesUnit.cs renamed to `IExtendedDokkaebiUnit` (extends the core IDokkaebiUnit interface with additional functionality)
- Removed duplicate `TurnPhase` enum from ITurnSystem.cs
- Consolidated all TurnPhase values in CommonEnums.cs

## Implementation Notes

The concrete implementations of these interfaces should be in their respective modules:
- Grid interfaces → Dokkaebi.Grid module
- Unit interfaces → Dokkaebi.Units module
- etc.

This separation allows modules to depend only on the Common assembly and not on each other directly. 