# Unity AI Project

## About
This project is a Unity-based prototype focused on implementing behavioral AI systems for non-player characters (NPCs).

The main goal of the project was to design scalable, efficient, and modular AI systems suitable for real-time applications.

## Features
- Finite State Machine (FSM) based AI
- Ground NPC navigation using grid-based system
- Flying NPC navigation using node-based A*-like algorithm
- Pathfinding optimization (route simplification)
- Multi-phase boss with behavior depending on health
- Modular and extensible architecture

## Technical Details
The project implements a modular architecture with separated systems for movement, behavior states, and combat logic.

Ground enemies use a local grid-based navigation system, while flying enemies use a node-based pathfinding system with a simplified A*-like approach.

Pathfinding was optimized by reducing unnecessary waypoints when movement direction remains consistent.

## Purpose
This project was created as a technical prototype, focusing on AI systems rather than full gameplay or content.

## Note
This is not a full game. The environment and gameplay are minimal, as the main focus was on AI implementation and system design.
