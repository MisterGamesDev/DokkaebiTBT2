# Zone System Migration Guide

## Overview

The `ZoneInstance` class previously existed in two different namespaces:
- `Dokkaebi.Grid.ZoneInstance` - Grid-focused implementation
- `Dokkaebi.Zones.ZoneInstance` - Effect-focused implementation

To eliminate confusion and code duplication, these classes have been merged. The `Dokkaebi.Zones.ZoneInstance` class now contains all functionality and is the one that should be used going forward.

## Migration Steps

1. **Update References**: Any code that was previously using `Dokkaebi.Grid.ZoneInstance` should be updated to use `Dokkaebi.Zones.ZoneInstance` instead.

2. **Namespace Changes**: Replace `using Dokkaebi.Grid;` with `using Dokkaebi.Zones;` where appropriate, or use the fully qualified name `Dokkaebi.Zones.ZoneInstance`.

3. **Method Changes**: The consolidated class includes all functionality from both versions, but some method signatures may have changed slightly. Here's a mapping of key methods:

| Old (Grid Namespace) | New (Zones Namespace) |
|----------------------|------------------------|
| `Initialize(ZoneProperties, List<GridPosition>, int playerId, int unitId)` | `Initialize(ZoneData, GridPosition, ...)` |
| `MoveZone(List<GridPosition>)` | `MoveZoneMultiTile(List<GridPosition>)` |
| `TickDuration()` | `ProcessTurn()` |
| `RemoveZone()` | `Deactivate()` or `ExpireZone()` |
| `ApplyEffectsToUnit(DokkaebiUnit)` | `ApplyZoneEffects()` |

4. **GridManager Integration**: The consolidated `ZoneInstance` class properly integrates with `GridManager` through the `RegisterWithGridManager()` and `UnregisterFromGridManager()` methods, which are called automatically.

5. **Multi-Tile Zones**: The consolidated class supports multi-tile zones through the `affectedPositions` list and the `MoveZoneMultiTile()` method.

## Deprecated Files

The original `Dokkaebi.Grid.ZoneInstance` class has been renamed to `ZoneInstance_DEPRECATED` and marked with the `[Obsolete]` attribute. This file will be removed in a future update.

## Tips for Smooth Migration

- If you encounter any compiler errors after the migration, check if you need to update namespace imports or method calls.
- The `GridManager` methods that deal with zones (`AddZoneToTile`, `RemoveZoneFromTile`, etc.) continue to work as before.
- If you have custom code that extends or interacts with zones, ensure it's updated to work with the consolidated class.

## Questions or Issues?

If you encounter any issues with the migration, please contact the technical lead for guidance. 