# AI System Documentation

## Overview

This project implements a behavioral AI system for NPCs in a Unity-based 2D environment.
The system is designed with a focus on modularity, scalability, and real-time performance.

The implementation includes:

* Enemy AI system (FSM-based)
* Navigation and pathfinding
* Flying and ground enemy logic
* Multi-phase boss system
* Health system and UI integration

---

## Enemy AI Architecture

The core of the AI system is built around a Finite State Machine (FSM).

### States

Each enemy operates using three main states:

* **Patrolling** — default idle behavior with random movement
* **Chasing** — active pursuit of the player
* **Searching** — searching last known player position

The FSM ensures predictable and controllable behavior while allowing complex interactions through state transitions.

### Base Class

All enemies inherit from a base `Enemy` class which provides:

* State management
* Player tracking (last seen position, cooldown)
* Knockback system
* Animation synchronization

This approach ensures code reuse and simplifies adding new enemy types.

---

## Navigation System

### Ground Enemies

Ground enemies use a **dynamic local grid system**:

* Grid is generated around each NPC
* Updates in real-time based on NPC position
* Supports obstacle detection and path planning

Key features:

* World ↔ Grid coordinate conversion
* Adaptive navigation without pre-baked maps
* Local environment awareness

### Flying Enemies

Flying enemies use a **node-based navigation system**:

* Independent from ground grid
* 2D spatial navigation
* Dynamic node generation

Additional logic:

* Collision checks using physics queries
* Walkable node detection
* Continuous grid updates

---

## Pathfinding

A custom **A*-like algorithm** is implemented for pathfinding.

### Key characteristics:

* Uses open/closed sets
* Calculates cost (gCost, hCost, fCost)
* Returns optimized path as world positions

### Optimizations:

* Simplified data structures (no priority queue)
* Path recalculation at fixed intervals
* **Path simplification**:

  * Removes unnecessary waypoints
  * Merges points with similar direction
  * Reduces computational overhead

This allows efficient real-time pathfinding even with multiple agents.

---

## Movement Logic

### Ground Movement

Ground enemies:

* Analyze terrain ahead
* Detect obstacles, gaps, and platforms
* Decide between:

  * Moving forward
  * Jumping up/down
  * Changing direction

System includes:

* Context-based movement rules
* Platform navigation logic
* Anti-stuck mechanisms (fallback behavior)

### Flying Movement

Flying enemies:

* Maintain optimal altitude
* Adjust vertical movement dynamically
* Use smooth velocity transitions

Additional features:

* Height control system
* Look-ahead path following
* Visual tilt based on velocity

---

## Combat Behavior

### Melee Enemy

Implements a **multi-stage attack system**:

1. Wind-up (preparation)
2. Dash attack
3. Cooldown

Features:

* Direction locking at attack start
* Increased damage during dash
* Knockback scaling
* Balanced attack timing

---

## Boss System

### Architecture

The boss system is modular and phase-based.

Core components:

* `BossPhase` (abstract class)
* `BossPhaseManager`
* `IBossSubBehavior` interface

Each phase:

* Encapsulates its own logic
* Can be independently extended
* Has lifecycle methods:

  * EnterPhase
  * UpdatePhase
  * ExitPhase

---

### Phase System

Boss behavior changes based on health:

#### Phase 1 (Ground)

* Close-range attacks
* Movement-based combat
* Attack types:

  * Dash attack
  * Jump slam
  * Projectile spit

#### Phase 2 (Flying)

* Activated below 60% health
* Introduces aerial movement
* Bullet-hell mechanics

Attacks include:

* Spread projectiles
* Area denial attacks
* Bomb drops

---

### Phase Transitions

Managed by `BossPhaseManager`:

* Monitors boss health
* Switches phases dynamically
* Ensures proper cleanup between phases

---

## Health System

A universal health system is implemented for:

* Player
* Enemies
* Boss

### Components:

* `HealthComponent`
* `HealthData`
* Death handling system

### Features:

* Damage calculation (randomized)
* Defense handling
* Temporary invulnerability
* Event-based updates

---

## UI Integration

The UI system provides:

* Real-time health display
* Boss health bar
* Synchronization with gameplay events

---

## Design Principles

The project follows key software engineering principles:

* **Modularity** — systems are separated and reusable
* **Encapsulation** — behavior is isolated per component
* **Scalability** — easy to extend with new enemies or features
* **Real-time performance** — optimized for runtime execution

---

## Conclusion

This project demonstrates a complete AI system implementation in Unity, including:

* Behavior logic (FSM)
* Navigation and pathfinding
* Adaptive enemy behavior
* Complex boss mechanics

The system is designed as a technical prototype focused on AI and architecture rather than full gameplay content.
