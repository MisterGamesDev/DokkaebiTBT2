# Dokkaebi Zone System Overview

## Current Structure

The Dokkaebi zone system currently consists of two primary implementations:

### 1. ZoneInstance (Dokkaebi.Zones.ZoneInstance)

- **Purpose**: Complex zone implementation with rich features
- **Features**:
  - Damage/healing over time
  - Status effect application
  - Zone merging and resonance
  - Visual and audio feedback
  - Grid integration with multi-tile support
- **Data Source**: Uses `ZoneData` ScriptableObjects for configuration
- **Integration**: Works with both `GridManager` and `UnitManager`
- **Usage**: Used for gameplay effects and mechanics

### 2. Zone (Dokkaebi.Zones.Zone)

- **Purpose**: Simpler zone implementation with basic properties
- **Features**:
  - Position tracking
  - Duration tracking
  - Basic identification (ID, type)
- **Data Source**: Initialized with simple parameters (no ScriptableObjects)
- **Usage**: Appears to be used for more basic tracking or networking purposes

### 3. ZoneManager

- Manages both `ZoneInstance` and `Zone` objects
- Has separate methods for creating and tracking each type
- Maintains separate collections for each type:
  - `zonesByPosition`: Dictionary<GridPosition, List<ZoneInstance>>
  - `activeZones`: Dictionary<string, Zone>

## Recent Changes

1. Consolidated two versions of `ZoneInstance`:
   - `Dokkaebi.Grid.ZoneInstance` - Deprecated (now `ZoneInstance_DEPRECATED`)
   - `Dokkaebi.Zones.ZoneInstance` - Enhanced with all functionality

2. Added proper grid integration to `ZoneInstance`

## Recommendations for Future Work

### Option 1: Full Consolidation

Combine `Zone` and `ZoneInstance` into a single class hierarchy:

```
ZoneBase (abstract base class with common properties)
├── SimpleZone (for basic use cases, replaces Zone)
└── EffectZone (full-featured, replaces ZoneInstance)
```

Benefits:
- Unified API
- Clearer inheritance
- Easier to maintain

### Option 2: Adapter Pattern

Keep both implementations but create adapter methods to convert between them:

- Add methods to `ZoneManager` to convert a `Zone` to a `ZoneInstance` and vice versa
- Use these for interoperability when needed

Benefits:
- Minimal changes to existing code
- Backwards compatibility

### Option 3: Complete Replacement

Phase out the simpler `Zone` class entirely in favor of the more feature-rich `ZoneInstance`:

1. Update all code that uses `Zone` to use `ZoneInstance` instead
2. Update `ZoneManager` to only manage `ZoneInstance` objects
3. Remove the `Zone` class

Benefits:
- Simplified codebase
- No duplication

## Implementation Notes

When considering changes:

1. Check all usages of both zone types to understand the impact
2. Look for networking-related dependencies on `Zone`
3. Consider serialization implications for saved games
4. Test thoroughly to ensure no functionality is lost

## Timeline Suggestion

1. Near term: Document current usage patterns
2. Mid term: Choose consolidation approach
3. Long term: Implement the chosen approach with thorough testing

## Conclusion

The zone system would benefit from further consolidation, but the approach should be chosen based on the current usage patterns and future requirements. The most crucial step was already taken - consolidating the duplicate `ZoneInstance` classes. 