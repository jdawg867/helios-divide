# Helios Divide — Seele AI Development Brief

**Project:** Helios Divide  
**Repository:** `jdawg867/helios-divide`  
**Primary branch:** `main`  
**Primary developer:** Seele AI  
**Project role of ChatGPT:** architecture, planning, review, QA, prompt design, milestone control  
**Status:** Phase 0 — Project Foundation  

## Purpose

This document defines the permanent development rules Seele AI should follow while building **Helios Divide**.

The authoritative creative specification is:

`docs/HELIOS_DIVIDE_GAME_DESIGN.md`

Seele should use that document as the master game vision unless a later approved project document explicitly supersedes part of it.

## Development Philosophy

Do not attempt to build the full game in one generation.

The game must be built incrementally through small, testable milestones.

For every development task:

1. Read the relevant project documentation.
2. Inspect the existing project before changing it.
3. Preserve working features.
4. Implement only the requested scope.
5. Keep systems modular.
6. Avoid unnecessary dependencies.
7. Test the new work.
8. Report what changed.
9. Report anything that remains incomplete.
10. Leave the project in a runnable state.

## Original-IP Requirement

Helios Divide is an original intellectual property.

Do not copy or reproduce protected material from *The Expanse* or any other game, film, television series, novel, or franchise.

Do not reproduce characters, faction names, dialogue, story arcs, logos, ship designs, costumes, UI designs, unique technology, named locations, music, sound effects, visual assets, or recognizable scenes.

Broad genre concepts such as a colonized Solar System, zero-gravity combat, political tension, asteroid settlements, spaceships, EVA, and realistic space travel are allowed.

All specific execution must be original to Helios Divide.

## Source-Control Rules

GitHub is the source of truth.

Repository: `jdawg867/helios-divide`  
Stable branch: `main`

Do not develop major features directly on `main`.

Use branches such as:

- `feature/project-foundation`
- `feature/player-controller`
- `feature/perspective-switch`
- `feature/combat-foundation`
- `feature/zero-g`
- `feature/quest-system`
- `feature/meridian-station`
- `fix/<short-description>`

Do not rewrite public branch history unless specifically instructed.

Do not commit credentials, API keys, tokens, passwords, machine-specific secrets, large temporary build files, caches, or generated files that should be ignored.

## Architecture Rules

Gameplay systems must be modular.

Prefer clear separation between:

- Player input
- Character movement
- Camera
- Perspective switching
- Interaction
- Combat
- Weapons
- Damage
- Inventory
- Equipment
- Dialogue
- Quests
- Factions
- Reputation
- Crime
- AI
- Companions
- EVA
- Gravity
- Environmental pressure
- Oxygen
- Ship systems
- Save/load
- UI
- Audio

Avoid monolithic scripts or classes that own unrelated systems.

Avoid tight coupling between unrelated systems.

## Data-Driven Content

Where supported by the selected engine, prefer data assets/resources/configuration for:

- Weapons
- Armor
- Items
- Factions
- Reputation thresholds
- NPC definitions
- Dialogue
- Quests
- Locations
- Ship modules
- Mission rewards

Do not hard-code large amounts of content into gameplay classes.

## Player Architecture

The player must eventually support seamless first-person and third-person gameplay.

Do not build two independent player controllers.

Use one authoritative player state with interchangeable camera/presentation behavior.

Shared systems should include movement, health, inventory, equipment, weapons, interaction, quest state, reputation, damage, and save data.

Camera perspective should not duplicate gameplay state.

## Input Rules

All gameplay input must use the engine's supported input-action system.

Do not permanently hard-code keys into gameplay scripts.

Design for keyboard/mouse, controller, and future rebinding.

Initial action concepts:

- Move
- Look
- Jump
- Sprint
- Crouch
- Interact
- Fire
- Aim
- Reload
- Next weapon
- Previous weapon
- Perspective toggle
- Inventory
- Pause

EVA inputs will be added later.

## Coding Standards

Code should be readable, modular, consistently named, documented where behavior is non-obvious, and defensive around invalid references.

Avoid dead experimental code before completion.

Avoid premature optimization and unnecessary abstraction.

Do not add third-party packages unless they provide clear value and are compatible with the project's license and engine.

If a new dependency is needed, document why.

## Scene / Level Rules

Scenes or maps should have clear responsibilities.

Avoid putting all gameplay logic inside a level scene.

Reusable gameplay logic belongs in reusable components/classes/prefabs/blueprints or the engine equivalent.

Initial development should use gray-box geometry.

Do not spend significant effort on final art before gameplay foundations work.

## Art Rules

Initial development should use gray-box environments, placeholder original assets, simple materials, functional UI, and clear silhouettes.

Do not generate or import copyrighted franchise assets.

Long-term visual direction:

- Grounded
- Industrial
- Functional
- Used-future
- Plausible engineering
- Strong faction differentiation

## AI Rules

Enemy AI should be built in layers.

Initial AI only needs detection, target selection, basic movement, basic cover-ready architecture, shooting, losing target, and death.

Later milestones add cover, flanking, communication, retreat, suppression, zero-G behavior, surrender, and environmental response.

Do not overbuild AI during Phase 0.

## Physics Rules

The game eventually supports standard gravity, low gravity, zero gravity, vacuum, and pressurized spaces.

Architecture should not assume standard gravity always exists.

Phase 0 should begin with standard-gravity environments only.

## Save-System Rule

Systems that contain persistent player progression should be designed so their state can be serialized later.

Do not implement the full save system before the player foundation unless required.

Avoid storing critical game state only inside transient scene objects.

## Performance Rules

Target scalable PC performance from the beginning.

Avoid expensive logic every frame when event-driven behavior works, unbounded spawning, unnecessary physics objects, huge always-loaded worlds, excessive dynamic lights, and heavy AI when NPCs are inactive.

Favor hub-based/world-streamed expansion rather than one enormous always-loaded scene.

## Vertical Slice

The first major playable milestone is **Dead Freight**.

It will eventually contain:

- Meridian Station
- Player ship
- Abandoned cargo vessel
- EVA section
- Rhea Calder
- First/third-person switching
- Four weapons
- Basic enemy AI
- Inventory
- Dialogue
- Quest tracking
- Reputation
- Oxygen
- Zero-G
- Magnetic boots
- Airlocks
- Save/load

Do not implement all of these at once.

## Development Order

### Phase 0 — Foundation
1. Engine/project baseline
2. Folder/module structure
3. Input actions
4. Test level
5. Project documentation integration
6. Runnable empty foundation

### Phase 1 — Player
1. Standard-gravity movement
2. First-person camera
3. Third-person camera
4. Perspective switching
5. Basic interaction

### Phase 2 — Combat
1. Weapon interface/base
2. First pistol
3. Damage
4. Health
5. Target dummy
6. Enemy AI

### Phase 3 — RPG
1. Inventory
2. Equipment
3. Items
4. Dialogue
5. Quest system
6. Reputation

### Phase 4 — EVA
1. Vacuum state
2. Oxygen
3. Zero-G movement
4. Thrusters
5. Magnetic boots
6. Airlocks

### Phase 5 — Player Ship

### Phase 6 — Dead Freight Vertical Slice

### Phase 7 — Expansion

## Definition of Done

A development task is complete only when:

- The project launches.
- Existing functionality still works.
- The requested feature works.
- There are no known blocking errors.
- Inputs are properly configured.
- Relevant documentation is updated.
- New code is reasonably structured.
- Temporary debug hacks are removed or clearly marked.
- The exact changed files are reported.
- Testing performed is reported.

## Seele Completion Report

At the end of every task, Seele should report:

### Implemented
What was added.

### Files Changed
Exact files created or modified.

### How to Test
Step-by-step test procedure.

### Known Limitations
Anything intentionally not implemented.

### Risks / Technical Debt
Anything that may need attention later.

### Recommended Next Task
One logical next development step.

## Important Scope Rule

When given a task such as “Build the player controller,” do not also independently build space combat, full inventory, skill trees, multiplayer, crafting, procedural planets, full campaign, or final art.

Only implement the requested milestone and any minimal dependency necessary to make it function.

## Current Project Goal

The immediate objective is not to make a complete RPG.

The immediate objective is to create a clean, extensible, version-controlled game project that can support Helios Divide for years of incremental development.

The first proof that the architecture is working will be:

> A player loads into a gray-box test level, moves smoothly, looks around, and can later switch between first-person and third-person without duplicating player state.
