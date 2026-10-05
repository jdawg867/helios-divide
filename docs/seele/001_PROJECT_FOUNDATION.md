# Seele Task 001 — Project Foundation

**Project:** Helios Divide  
**Milestone:** Phase 0  
**Branch:** `feature/project-foundation`  
**Priority:** Critical  
**Scope:** Foundation only  

## Objective

Create the initial runnable game-project foundation for **Helios Divide**.

Do not begin building the full game.

Do not implement weapons, combat, enemies, quests, inventory, EVA, ships, multiplayer, or final art in this task.

The goal is to establish a clean project baseline that future Seele tasks can safely build upon.

## Required Reading

Before changing anything, read:

- `README.md`
- `docs/HELIOS_DIVIDE_GAME_DESIGN.md`
- `docs/SEELE_AI_DEVELOPMENT_BRIEF.md`

These documents define the game vision and development rules.

## Source Control

Work on:

`feature/project-foundation`

Do not develop directly on `main`.

Start from the latest `main`.

## Required Work

### 1. Initialize the Game Project

Create the appropriate game-engine project files using the engine/environment available through Seele.

Use a production-suitable 3D project configuration.

Do not create a throwaway prototype separate from the repository.

The game project must live inside this repository.

### 2. Create a Clean Project Structure

Use the engine's conventions, but create logical locations for:

- Core
- Player
- Characters
- Camera
- Interaction
- Combat
- Items
- Inventory
- AI
- Factions
- Quests
- Ships
- World
- UI
- Audio
- Data
- Tests or automated validation if supported

Do not populate these systems yet beyond what the foundation requires.

### 3. Configure Input Actions

Create input actions for future use:

- Move
- Look
- Jump
- Sprint
- Crouch
- Interact
- Fire
- Aim
- Reload
- NextWeapon
- PreviousWeapon
- TogglePerspective
- Inventory
- Pause

Keyboard/mouse mappings should be sensible.

Controller mappings should be configured where straightforward.

Do not implement full gameplay for all actions yet.

### 4. Create a Bootstrap / Startup Flow

Create a reliable application startup path.

On launch, the project should enter a simple development test map/scene.

Avoid requiring developers to manually select a scene each time.

### 5. Create a Gray-Box Foundation Test Level

Create a minimal test environment containing:

- Floor
- Several walls
- One raised platform
- One ramp or stairs
- A few simple geometry obstacles
- Basic lighting
- A clearly marked player spawn location

No final art.

No copyrighted assets.

Recommended name:

`FoundationTest`

or the engine-equivalent naming convention.

### 6. Add Project Identity

Where supported:

Game name:

`Helios Divide`

Internal project identifier should use an appropriate normalized form such as:

`HeliosDivide`

Do not use *The Expanse* in package names, namespaces, UI, or project metadata.

### 7. Establish Debug/Development Conventions

Where appropriate, add a lightweight development logging convention.

Avoid excessive logging.

Errors should be meaningful.

### 8. Preserve Documentation

Do not delete or move:

- `README.md`
- `docs/HELIOS_DIVIDE_GAME_DESIGN.md`
- `docs/SEELE_AI_DEVELOPMENT_BRIEF.md`
- `docs/seele/001_PROJECT_FOUNDATION.md`

The repository documentation must remain readable outside the engine.

### 9. Update `.gitignore`

Update `.gitignore` appropriately for the selected engine.

Exclude build output, cache directories, IDE temporary files, generated intermediate files, local-only configuration, credentials, and secrets.

Do not ignore source assets or files required to reproduce the project.

### 10. Validate the Project

Before declaring completion:

- Launch the project.
- Confirm the test level loads.
- Confirm there are no blocking startup errors.
- Confirm the project can be reopened.
- Confirm version-controlled project files are sufficient to reproduce the project.
- Confirm generated/cache files are not unnecessarily tracked.

## Explicitly Out of Scope

Do **not** implement:

- Player movement logic
- First-person camera gameplay
- Third-person camera gameplay
- Perspective switching
- Weapons
- Shooting
- Enemies
- Inventory
- Quests
- Reputation
- Spacecraft
- EVA
- Zero gravity
- Oxygen
- Multiplayer
- Procedural generation
- Final environment art
- Character art
- Cinematics

Those are future tasks.

## Acceptance Criteria

Task 001 is complete when:

1. The Helios Divide game project opens successfully.
2. The project launches into the gray-box `FoundationTest` level.
3. Core folders/modules exist.
4. Input actions are configured.
5. The project has correct Helios Divide identity.
6. The `.gitignore` matches the selected engine.
7. No major gameplay system has been prematurely implemented.
8. The project is clean enough to begin the player-controller milestone.
9. Repository documentation remains intact.
10. Seele provides a completion report.

## Required Completion Report

Return:

### Engine / Version
State exactly what engine and version were used.

### Implemented
Summarize foundation work.

### Files Changed
List all created/modified files.

### Startup Path
Explain how the project starts and which test map loads.

### Input Actions
List configured inputs.

### Validation
Describe what was actually tested.

### Known Limitations
List anything intentionally deferred.

### Repository Status
State branch and whether the working tree is clean.

### Recommended Next Task
The next task should normally be:

`Task 002 — Standard-Gravity Player Controller`

Do not begin Task 002 until Task 001 is reviewed and approved.
