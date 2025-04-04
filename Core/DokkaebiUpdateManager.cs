using System;
using System.Collections.Generic;
using UnityEngine;
using Dokkaebi.Utilities;
using Dokkaebi.Interfaces;

namespace Dokkaebi.Core
{
    /// <summary>
    /// Central update manager for all Dokkaebi systems to reduce Update() overhead
    /// </summary>
    public class DokkaebiUpdateManager : MonoBehaviour, ICoreUpdateService
    {
        private static DokkaebiUpdateManager instance;
        public static DokkaebiUpdateManager Instance 
        {
            get
            {
                if (instance == null)
                {
                    GameObject obj = new GameObject("DokkaebiUpdateManager");
                    instance = obj.AddComponent<DokkaebiUpdateManager>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        // Observer interfaces
        public interface IUpdateObserver : Dokkaebi.Interfaces.IUpdateObserver { void CustomUpdate(float deltaTime); }
        public interface IFixedUpdateObserver { void CustomFixedUpdate(float deltaTime); }
        public interface ILateUpdateObserver { void CustomLateUpdate(float deltaTime); }

        // Observer collections
        private readonly List<IUpdateObserver> updateObservers = new List<IUpdateObserver>();
        private readonly List<IFixedUpdateObserver> fixedUpdateObservers = new List<IFixedUpdateObserver>();
        private readonly List<ILateUpdateObserver> lateUpdateObservers = new List<ILateUpdateObserver>();
        
        // Buffered lists to handle modifications during iteration
        private readonly List<IUpdateObserver> pendingAddUpdateObservers = new List<IUpdateObserver>();
        private readonly List<IUpdateObserver> pendingRemoveUpdateObservers = new List<IUpdateObserver>();
        private readonly List<IFixedUpdateObserver> pendingAddFixedObservers = new List<IFixedUpdateObserver>();
        private readonly List<IFixedUpdateObserver> pendingRemoveFixedObservers = new List<IFixedUpdateObserver>();
        private readonly List<ILateUpdateObserver> pendingAddLateObservers = new List<ILateUpdateObserver>();
        private readonly List<ILateUpdateObserver> pendingRemoveLateObservers = new List<ILateUpdateObserver>();
        
        private bool isUpdating = false;
        private bool isFixedUpdating = false;
        private bool isLateUpdating = false;

        private void Awake()
{
    Debug.Log($"[UpdateManager] Awake() called on GameObject: {gameObject.name} (InstanceID: {gameObject.GetInstanceID()})"); // Add this log

    // Check if an instance already exists AND it's not this instance
    if (instance != null && instance != this)
    {
        Debug.LogError($"[UpdateManager] DUPLICATE INSTANCE DETECTED on {gameObject.name} (InstanceID: {gameObject.GetInstanceID()})! Destroying this duplicate. The original singleton is on {instance.gameObject.name} (InstanceID: {instance.gameObject.GetInstanceID()}).");
        Destroy(gameObject); // Destroy this GameObject because it's a duplicate
        return;
    }
    // If no instance exists yet, this one becomes the singleton
    else if (instance == null)
    {
        Debug.Log($"[UpdateManager] Setting static instance to {gameObject.name} (InstanceID: {gameObject.GetInstanceID()}). Applying DontDestroyOnLoad.");
        instance = this;
        // IMPORTANT: Only apply DontDestroyOnLoad if the GameObject is a root object
        if (transform.parent == null)
        {
             DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning($"[UpdateManager] {gameObject.name} is not a root object. DontDestroyOnLoad was NOT applied. Ensure the UpdateManager is on a root GameObject.", gameObject);
        }
    }
    // Else: instance == this, which means Awake() was called again on the original singleton (e.g., scene reload), do nothing special.
    // Initialization that should only happen once for the singleton should go here or in Start() after the singleton check.
    // Example: InitializeDataLookups(); if moved from another script or needed here.
}

        
        private void Update()
        {
            //Debug.Log($"[UpdateManager] Update() frame {Time.frameCount}. Observer Count Before Processing: {updateObservers.Count}, Pending Adds: {pendingAddUpdateObservers.Count}, Pending Removes: {pendingRemoveUpdateObservers.Count}");
            // Process any pending changes first
            ProcessPendingObserverChanges();
            
            isUpdating = true;
            float deltaTime = Time.deltaTime;
            
            // Use for loop for performance (avoid allocation)
            for (int i = 0; i < updateObservers.Count; i++)
            {
                try
                {
                    updateObservers[i].CustomUpdate(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in CustomUpdate for {updateObservers[i]}: {e}");
                }
            }
            
            isUpdating = false;
            
            // Process any observer changes that occurred during update
            ProcessPendingObserverChanges();
        }
        
        private void FixedUpdate()
        {
            // Process any pending changes first
            ProcessPendingFixedObserverChanges();
            
            isFixedUpdating = true;
            float deltaTime = Time.fixedDeltaTime;
            
            for (int i = 0; i < fixedUpdateObservers.Count; i++)
            {
                try
                {
                    fixedUpdateObservers[i].CustomFixedUpdate(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in CustomFixedUpdate for {fixedUpdateObservers[i]}: {e}");
                }
            }
            
            isFixedUpdating = false;
            
            // Process any observer changes that occurred during update
            ProcessPendingFixedObserverChanges();
        }
        
        private void LateUpdate()
        {
            // Process any pending changes first
            ProcessPendingLateObserverChanges();
            
            isLateUpdating = true;
            float deltaTime = Time.deltaTime;
            
            for (int i = 0; i < lateUpdateObservers.Count; i++)
            {
                try
                {
                    lateUpdateObservers[i].CustomLateUpdate(deltaTime);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error in CustomLateUpdate for {lateUpdateObservers[i]}: {e}");
                }
            }
            
            isLateUpdating = false;
            
            // Process any observer changes that occurred during update
            ProcessPendingLateObserverChanges();
        }
        
        private void ProcessPendingObserverChanges()
{
    int initialCount = updateObservers.Count;

    if (pendingAddUpdateObservers.Count > 0)
    {
        // v-- ADD/CONFIRM THIS LOG --v
        Debug.Log($"[UpdateManager InstanceID: {gameObject.GetInstanceID()}] Processing Pending Adds: Adding {pendingAddUpdateObservers.Count} observers to main list.");
        updateObservers.AddRange(pendingAddUpdateObservers);
        pendingAddUpdateObservers.Clear();
    }

    if (pendingRemoveUpdateObservers.Count > 0)
    {
        // v-- ADD/CONFIRM THIS LOG --v
         Debug.Log($"[UpdateManager InstanceID: {gameObject.GetInstanceID()}] Processing Pending Removes: Removing {pendingRemoveUpdateObservers.Count} observers.");
        // ... (removal logic) ...
    }

    // v-- ADD/CONFIRM THIS LOG --v
    if (updateObservers.Count != initialCount) {
         Debug.Log($"[UpdateManager InstanceID: {gameObject.GetInstanceID()}] Observer count changed from {initialCount} to {updateObservers.Count} after processing pending changes.");
    } else if (pendingAddUpdateObservers.Count > 0 || pendingRemoveUpdateObservers.Count > 0) {
        // Log even if count didn't change but pending lists were processed (might indicate an issue)
        Debug.Log($"[UpdateManager InstanceID: {gameObject.GetInstanceID()}] Processed pending lists, but observer count remained {updateObservers.Count}.");
    }
}
        
        private void ProcessPendingFixedObserverChanges()
        {
            // Add pending observers
            if (pendingAddFixedObservers.Count > 0)
            {
                fixedUpdateObservers.AddRange(pendingAddFixedObservers);
                pendingAddFixedObservers.Clear();
            }
            
            // Remove pending observers
            if (pendingRemoveFixedObservers.Count > 0)
            {
                foreach (var observer in pendingRemoveFixedObservers)
                {
                    fixedUpdateObservers.Remove(observer);
                }
                pendingRemoveFixedObservers.Clear();
            }
        }
        
        private void ProcessPendingLateObserverChanges()
        {
            // Add pending observers
            if (pendingAddLateObservers.Count > 0)
            {
                lateUpdateObservers.AddRange(pendingAddLateObservers);
                pendingAddLateObservers.Clear();
            }
            
            // Remove pending observers
            if (pendingRemoveLateObservers.Count > 0)
            {
                foreach (var observer in pendingRemoveLateObservers)
                {
                    lateUpdateObservers.Remove(observer);
                }
                pendingRemoveLateObservers.Clear();
            }
        }
        
        // Registration methods for Update
       public void RegisterUpdateObserver(IUpdateObserver observer)
{
    if (observer == null) return;

    // Add this log line from one of the previous debugging steps if you still want confirmation registration is attempted:
    // string observerName = (observer as MonoBehaviour)?.gameObject.name ?? observer.GetType().Name;
    // int instanceId = (observer as MonoBehaviour)?.gameObject.GetInstanceID() ?? 0;
    // Debug.Log($"[UpdateManager InstanceID: {gameObject.GetInstanceID()}] RegisterUpdateObserver called for: {observerName} (InstanceID: {instanceId}). isUpdating={isUpdating}");


    if (isUpdating) // Check if currently inside the Update loop
    {
        // If called during the update loop, add to a pending list to avoid modifying the collection while iterating
        if (!updateObservers.Contains(observer) && !pendingAddUpdateObservers.Contains(observer))
        {
            pendingAddUpdateObservers.Add(observer);
        }
    }
    else if (!updateObservers.Contains(observer)) // If not updating, and not already in the list, add directly
    {
        updateObservers.Add(observer);
    }
    // Optional: Add a warning log here if the observer was already in the list, like in the debug versions.
    // else
    // {
    //     Debug.LogWarning($"[UpdateManager InstanceID: {gameObject.GetInstanceID()}] Observer {observerName} (InstanceID: {instanceId}) already exists in main list.");
    // }
}
        
        public void UnregisterUpdateObserver(IUpdateObserver observer)
        {
            if (observer == null) return;
            
            if (isUpdating)
            {
                if (updateObservers.Contains(observer) && !pendingRemoveUpdateObservers.Contains(observer))
                {
                    pendingRemoveUpdateObservers.Add(observer);
                }
            }
            else
            {
                updateObservers.Remove(observer);
            }
        }
        
        // Registration methods for FixedUpdate
        public void RegisterFixedUpdateObserver(IFixedUpdateObserver observer)
        {
            if (observer == null) return;
            
            if (isFixedUpdating)
            {
                if (!fixedUpdateObservers.Contains(observer) && !pendingAddFixedObservers.Contains(observer))
                {
                    pendingAddFixedObservers.Add(observer);
                }
            }
            else if (!fixedUpdateObservers.Contains(observer))
            {
                fixedUpdateObservers.Add(observer);
            }
        }
        
        public void UnregisterFixedUpdateObserver(IFixedUpdateObserver observer)
        {
            if (observer == null) return;
            
            if (isFixedUpdating)
            {
                if (fixedUpdateObservers.Contains(observer) && !pendingRemoveFixedObservers.Contains(observer))
                {
                    pendingRemoveFixedObservers.Add(observer);
                }
            }
            else
            {
                fixedUpdateObservers.Remove(observer);
            }
        }
        
        // Registration methods for LateUpdate
        public void RegisterLateUpdateObserver(ILateUpdateObserver observer)
        {
            if (observer == null) return;
            
            if (isLateUpdating)
            {
                if (!lateUpdateObservers.Contains(observer) && !pendingAddLateObservers.Contains(observer))
                {
                    pendingAddLateObservers.Add(observer);
                }
            }
            else if (!lateUpdateObservers.Contains(observer))
            {
                lateUpdateObservers.Add(observer);
            }
        }
        
        public void UnregisterLateUpdateObserver(ILateUpdateObserver observer)
        {
            if (observer == null) return;
            
            if (isLateUpdating)
            {
                if (lateUpdateObservers.Contains(observer) && !pendingRemoveLateObservers.Contains(observer))
                {
                    pendingRemoveLateObservers.Add(observer);
                }
            }
            else
            {
                lateUpdateObservers.Remove(observer);
            }
        }

        // ICoreUpdateService implementation
        void ICoreUpdateService.RegisterUpdateObserver(Dokkaebi.Interfaces.IUpdateObserver observer)
        {
            if (observer is IUpdateObserver updateObserver)
            {
                RegisterUpdateObserver(updateObserver);
            }
        }

        void ICoreUpdateService.UnregisterUpdateObserver(Dokkaebi.Interfaces.IUpdateObserver observer)
        {
            if (observer is IUpdateObserver updateObserver)
            {
                UnregisterUpdateObserver(updateObserver);
            }
        }
    }
}