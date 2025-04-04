using System.Collections.Generic;
using UnityEngine;

public class ZoneManager
{
    public void ApplyZoneEffects(List<ZoneInstance> positionZones)
    {
        foreach (ZoneInstance zone in positionZones)
        {
            if (zone != null && zone.IsActive)
            {
                zone.ApplyZoneEffects();
            }
        }
    }

    public void ProcessTurn(List<ZoneInstance> positionZones)
    {
        for (int i = positionZones.Count - 1; i >= 0; i--)
        {
            ZoneInstance zone = positionZones[i];
            if (zone.IsActive)
            {
                zone.ProcessTurn();
            }
            else
            {
                positionZones.RemoveAt(i);
            }
        }
    }

    public void DeactivateZone(ZoneInstance zone)
    {
        if (zone != null && zone.IsActive)
        {
            zone.Deactivate();
        }
    }

    public bool MergeZones(ZoneInstance existingZone, ZoneInstance newZone)
    {
        if (existingZone == null || !existingZone.IsActive || !newZone.IsActive)
        {
            return false;
        }

        if (newZone.CanMergeWith(existingZone))
        {
            newZone.MergeWith(existingZone);
            return true;
        }

        return false;
    }

    public bool IsValidPosition(Vector3 newPosition)
    {
        // Implementation of IsValidPosition method
        return true; // Placeholder return, actual implementation needed
    }

    public bool TryPlaceZone(ZoneInstance zone, Vector3 newPosition)
    {
        if (zone == null || !zone.IsActive || !IsValidPosition(newPosition))
        {
            return false;
        }

        // Implementation of placing the zone at the new position
        return true; // Placeholder return, actual implementation needed
    }

    /// <summary>
    /// Create a new zone at the specified position
    /// </summary>
    public Zone CreateZone(string zoneType, Vector2Int position, int size, int duration, string ownerUnitId = "")
    {
        // ... existing code ...
    }

    /// <summary>
    /// Convert a simple Zone object (network state representation) into a full gameplay ZoneInstance
    /// </summary>
    public ZoneInstance CreateInstanceFromZone(Zone zone)
    {
        if (zone == null)
        {
            Debug.LogWarning("Cannot create ZoneInstance from null Zone");
            return null;
        }

        // Get zone data from DataManager based on zone type
        var zoneData = DataManager.Instance.GetZoneData(zone.ZoneType);
        if (zoneData == null)
        {
            Debug.LogWarning($"Failed to find ZoneData for type {zone.ZoneType}");
            return null;
        }

        // Convert Vector2Int position to GridPosition
        var gridPosition = Dokkaebi.Interfaces.GridPosition.FromVector2Int(zone.Position);

        // Instantiate the zone instance prefab
        GameObject zoneObject = Instantiate(zoneInstancePrefab, Vector3.zero, Quaternion.identity, zonesParent);
        zoneObject.name = $"ZoneInstance_{zone.ZoneType}_{zone.ZoneId}";

        // Get or add ZoneInstance component
        ZoneInstance zoneInstance = zoneObject.GetComponent<ZoneInstance>();
        if (zoneInstance == null)
        {
            zoneInstance = zoneObject.AddComponent<ZoneInstance>();
        }

        // Convert owner unit ID from string to int
        int ownerUnitId = -1;
        if (!string.IsNullOrEmpty(zone.OwnerUnitId))
        {
            if (!int.TryParse(zone.OwnerUnitId, out ownerUnitId))
            {
                Debug.LogWarning($"Failed to parse owner unit ID: {zone.OwnerUnitId}");
                ownerUnitId = -1;
            }
        }

        // Initialize the zone instance
        zoneInstance.Initialize(
            zoneData,
            gridPosition,
            ownerUnitId,
            zone.RemainingDuration
        );

        // Add to position tracking
        if (!zonesByPosition.TryGetValue(gridPosition, out var zonesAtPosition))
        {
            zonesAtPosition = new List<ZoneInstance>();
            zonesByPosition[gridPosition] = zonesAtPosition;
        }
        zonesAtPosition.Add(zoneInstance);

        return zoneInstance;
    }

    /// <summary>
    /// Convert a gameplay ZoneInstance into a simpler Zone object (network state representation)
    /// </summary>
    public Zone CreateZoneFromInstance(ZoneInstance instance)
    {
        if (instance == null)
        {
            Debug.LogWarning("Cannot create Zone from null ZoneInstance");
            return null;
        }

        // Generate a unique ID for the zone
        string zoneId = System.Guid.NewGuid().ToString();

        // Convert GridPosition to Vector2Int
        var position = new Vector2Int(instance.Position.x, instance.Position.z);

        // Create the zone GameObject
        GameObject zoneObject = Instantiate(zoneInstancePrefab, Vector3.zero, Quaternion.identity, zonesParent);
        zoneObject.name = $"Zone_{instance.DisplayName}_{zoneId.Substring(0, 8)}";

        // Get or add Zone component
        Zone zone = zoneObject.GetComponent<Zone>();
        if (zone == null)
        {
            zone = zoneObject.AddComponent<Zone>();
        }

        // Initialize zone data
        zone.Initialize(
            zoneId,
            instance.DisplayName, // Using DisplayName as ZoneType
            position,
            instance.Radius,
            instance.RemainingDuration,
            instance.OwnerUnitId.ToString()
        );

        // Set position
        zone.SetPosition(position);

        // Add to active zones
        activeZones[zoneId] = zone;

        return zone;
    }

    /// <summary>
    /// Get the appropriate zone prefab based on zone type
    /// </summary>
    private GameObject GetZonePrefabByType(string zoneType)
    {
        // ... existing code ...
    }
} 