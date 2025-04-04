# Dokkaebi - Project Overview

## 1. Introduction

Dokkaebi is a 3D Turn-Based Strategy game designed for PvP combat, intended for PC (Steam) and Mobile (iOS/Android) with cross-play [cite: GDD, Architectural Plan]. Matches involve 6v6 squad combat on a 10x10 grid map [cite: GDD, Architectural Plan].

## 2. Core Concept

Players command units called Dokkaebi, defined by a combination of **Origin** (elemental/spiritual traits, e.g., Hydra, Blaze) and **Calling** (combat role, e.g., Duelist, Guardian) [cite: GDD, Architectural Plan]. Gameplay revolves around strategic movement, ability usage fueled by **Aura**, activating powerful **Overload** states using **Mastery Points (MP)**, and manipulating the battlefield through **Zones** [cite: GDD, Architectural Plan]. The map starts blank, with dynamics emerging from player actions [cite: Architectural Plan].

## 3. Technology Stack

* **Engine:** Unity 6 (Version 6000.0.36f1)
* **Language:** C#
* **Pathfinding:** A* Pathfinding Project
* **Networking:** Planned authoritative server model, potentially using PlayFab and Azure Functions (basic stubs/managers currently exist) [cite: Architectural Plan, Scripts/Dokkaebi/Core/Networking/NetworkingManager.cs]. Specific library (e.g., Netcode for GameObjects) recommended but not yet finalized [cite: Architectural Plan].
* **Input:** Unity Input System Package [cite: Architectural Plan].
* **UI:** Unity UI (Canvas, TextMeshPro) or potentially UI Toolkit [cite: Architectural Plan, Scripts/UI/UIManager.cs].

## 4. Key Features (Prototype Scope)

* **Core Combat Loop:** Turn-based actions, ability execution, unit elimination.
* **Turn System (DTFS):** Simultaneous Movement Phase, distinct Aura Phases for each player [cite: GDD, Architectural Plan, Scripts/Dokkaebi/Core/DokkaebiTurnSystemCore.cs].
* **Units:** 6v6 setup with basic Origin/Calling definitions [cite: GDD, Architectural Plan]. HP, Aura, MP tracking [cite: Scripts/Dokkaebi/Units/DokkaebiUnit.cs].
* **Movement:** Grid-based movement with pathfinding (via A*) and simultaneous resolution including conflict handling [cite: Architectural Plan, Scripts/Dokkaebi/Pathfinding/DokkaebiMovementHandler.cs, Scripts/Dokkaebi/Core/MovementManager (from Arch Plan)].
* **Abilities:** Basic offensive, defensive, utility, and zone creation abilities with Aura costs and cooldowns [cite: GDD, Architectural Plan]. Overload state check (MP >= 7) [cite: Architectural Plan, Scripts/Dokkaebi/Units/DokkaebiUnit.cs]. Tactical Repositioning [cite: Architectural Plan, Scripts/Dokkaebi/Core/Networking/Commands/RepositionCommand.cs]. Simplified "Rewind" [cite: Architectural Plan].
* **Zones:** Basic creation, duration, stacking, merging, "Terrain Shift", and simplified "Unstable Resonance" / "Void Space" mechanics [cite: Architectural Plan, Scripts/Dokkaebi/Zones/ZoneManager.cs, Scripts/Dokkaebi/Zones/ZoneInstance.cs].
* **Resources:** Aura gain/consumption, MP gain tracking [cite: GDD, Architectural Plan].
* **Win Conditions:** Eliminate all enemy units or have more units at match timer expiration [cite: Architectural Plan].
* **Networking:** Foundational authoritative client-server structure with command pattern [cite: Architectural Plan, Scripts/Dokkaebi/Core/PlayerActionManager.cs, Scripts/Dokkaebi/Core/Networking/NetworkingManager.cs].

## 5. Setup & Running

*(TODO: Add specific Unity version download link, project cloning instructions, any necessary configuration steps, and how to build/run the prototype).*

## 6. Contribution Guidelines

*(TODO: Add guidelines if applicable, e.g., branching strategy, pull request process, code review standards).*