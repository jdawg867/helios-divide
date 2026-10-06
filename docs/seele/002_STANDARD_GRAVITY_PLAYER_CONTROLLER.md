# Seele Task 002 — Standard-Gravity Player Controller + Multi-Device Input

**Project:** Helios Divide
**Engine:** Unity 6000.0.49f1 (Unity 6)
**Renderer:** URP 17.0.4
**Input:** Unity Input System 1.14.0
**Branch:** `feature/player-controller`
**Start point:** latest approved `main`

## Objective

Build the first playable player-controller foundation for Helios Divide.

The player must be able to load `FoundationTest`, move, look, jump, sprint, and crouch using:

- Keyboard + mouse
- Xbox Series X|S controller
- Nintendo Switch Pro Controller

Use Unity's Input System and keep gameplay code device-neutral.

Task 002 may use one simple development first-person view to validate locomotion and look, but it must not implement the final FPS/TPS camera system.

## Required Reading

Read before changing anything:

- `README.md`
- `docs/HELIOS_DIVIDE_GAME_DESIGN.md`
- `docs/SEELE_AI_DEVELOPMENT_BRIEF.md`
- `docs/seele/001_PROJECT_FOUNDATION.md`
- `docs/seele/REPOSITORY_HYGIENE.md`
- this task

Preserve the approved Task 001 foundation and Seele runtime/editor files documented in repository hygiene.

## Source Control

Start from latest `main`.

Create and work only on:

`feature/player-controller`

Do not work directly on `main`.
Do not merge.
Do not begin Task 003.

When complete, commit and push, then stop for review.

Recommended commit message:

`Add standard-gravity player controller`

## Architecture

### One authoritative player

Create one authoritative player object/state.

Do not create separate first-person and third-person characters. Future camera modes must use the same player state.

### Modular components

Keep responsibilities separated. A reasonable split is:

- input/action reader
- locomotion
- look/development camera
- grounded state
- active-device tracking

Do not put unrelated behavior into one large script.

### Input System only

Do not use `Input.GetAxis`, `Input.GetKey`, the old Input Manager, or vendor-specific button polling.

Use:

`Assets/HeliosDivide/Core/HeliosDivideInput.inputactions`

Gameplay code consumes logical actions (`Move`, `Look`, `Jump`, `Sprint`, `Crouch`) rather than Xbox/Nintendo button names.

## Player Prefab

Create:

`Assets/HeliosDivide/Player/HDPlayer.prefab`

It should contain at minimum:

- root GameObject
- `CharacterController`
- input component(s)
- locomotion component(s)
- camera pivot
- temporary development camera
- optional placeholder body/capsule

The prefab must be reusable in future scenes and not depend directly on `FoundationTest`.

## Standard-Gravity Locomotion

Use Unity `CharacterController` unless there is a documented technical reason not to.

Required:

- walk
- analog walk
- sprint
- crouch
- jump
- gravity
- grounded detection
- slope handling
- step handling
- collision with gray-box geometry

Movement must be relative to horizontal look direction.

Preserve analog left-stick magnitude. Do not normalize every non-zero stick input to full speed.

Prevent faster diagonal keyboard movement.

Suggested initial configurable values:

- walk speed: `4.5 m/s`
- sprint speed: `7.0 m/s`
- crouch speed: `2.5 m/s`
- acceleration: `18–25 m/s²`
- deceleration: `20–30 m/s²`
- jump height: about `1.2 m`
- gravity: about `-20 m/s²`

Expose tuning values; do not bury them as magic numbers.

### Sprint

Sprint requires meaningful movement input, works on keyboard/gamepad, and ends when sprint input is released.

No stamina yet.

### Jump

Jump only while grounded. No double jump, mantling, vaulting, or low-gravity behavior.

### Crouch

Crouch must:

- reduce controller height
- lower camera appropriately
- reduce movement speed
- work on keyboard/controller
- prevent standing if geometry blocks standing height

Hold or toggle is acceptable; document which was chosen.

## Grounding / Collision

Validate against the existing floor, ramp, stairs, raised platform, walls, and obstacles.

The player must not fall through floors, pass through walls, jitter excessively on ramps/stairs, or accumulate uncontrolled downward velocity while grounded.

Use `CharacterController` slope/step behavior; do not write a custom physics engine.

## Development Look Camera

For Task 002 only:

- yaw rotates player orientation
- pitch rotates a camera pivot
- clamp pitch
- mouse look
- right-stick look
- lock/hide cursor during gameplay

Do not implement third-person camera, shoulder switching, perspective toggle, camera collision, recoil, head bob, shake, or final FOV settings.

### Mouse vs gamepad look

Treat mouse delta and gamepad stick correctly as different input types.

- Mouse: configurable mouse sensitivity; do not incorrectly multiply raw mouse delta by deltaTime.
- Gamepad: right stick is a look rate; apply time-based rotation; separate controller sensitivity.

## Controller Support

Required devices:

- Keyboard + mouse
- Xbox Series X|S controller
- Nintendo Switch Pro Controller

Paired Joy-Con is optional only if Unity exposes the pair as one usable `Gamepad`. Single Joy-Con is out of scope.

Prefer generic Input System paths such as:

- `<Gamepad>/leftStick`
- `<Gamepad>/rightStick`
- `<Gamepad>/buttonSouth`
- `<Gamepad>/buttonEast`
- `<Gamepad>/buttonWest`
- `<Gamepad>/buttonNorth`
- `<Gamepad>/leftStickPress`
- `<Gamepad>/leftShoulder`
- `<Gamepad>/rightShoulder`
- `<Gamepad>/leftTrigger`
- `<Gamepad>/rightTrigger`
- `<Gamepad>/dpad/up`
- `<Gamepad>/dpad/down`
- `<Gamepad>/start`

### Physical face-button convention

| Action | Xbox Series | Switch Pro |
|---|---|---|
| Jump | A | B |
| Crouch | B | A |
| Interact (future) | X | Y |
| Reload (future) | Y | X |
| Sprint | Left Stick Press | Left Stick Press |
| Aim (future) | LT | ZL |
| Fire (future) | RT | ZR |
| Previous Weapon (future) | LB | L |
| Next Weapon (future) | RB | R |
| Toggle Perspective (future) | D-Pad Down | D-Pad Down |
| Inventory (future) | D-Pad Up | D-Pad Up |
| Pause | Menu | Plus |

Do not add vendor-specific gameplay polling.

## Dead Zones

Use sensible Input System dead-zone processors/settings.

Requirements:

- no obvious movement drift
- no obvious camera drift
- analog range preserved beyond the dead zone
- dead-zone values easy to tune later

Document selected values/processors.

## Runtime Device Switching

During one Play Mode session, the player must be able to:

1. move with WASD
2. immediately move with left stick
3. look with mouse
4. immediately look with right stick
5. return to keyboard/mouse

No respawn or reload.

Add a lightweight active-device category abstraction with at least:

- `KeyboardMouse`
- `Gamepad`

Do not require gameplay code to identify Xbox vs Nintendo.

## Preserve Input Actions

Do not remove existing actions:

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

Only locomotion/look actions need active gameplay behavior in Task 002.

## FoundationTest Integration

Update `FoundationTest` so it uses the reusable player prefab at `PlayerSpawn`.

The old static camera may be removed/disabled if replaced by the player camera.

Keep the existing gray-box environment and lighting.

## Configuration

Expose at minimum:

- walk speed
- sprint speed
- crouch speed
- acceleration
- deceleration
- jump height
- gravity
- mouse sensitivity
- controller sensitivity
- pitch limit

Serialized fields or a `ScriptableObject` are both acceptable.

## Testing

At minimum validate automatically where practical:

- player prefab exists
- required player components exist
- required input actions still exist
- `FoundationTest` contains/references a valid player
- project Input Actions reference remains valid

If practical, add Play Mode tests for forward movement, grounded-only jumping, crouch height, and blocked standing.

### Manual keyboard/mouse

Verify:

- WASD movement
- diagonal speed capped correctly
- mouse look
- Space jump
- Left Shift sprint
- Left Ctrl/C crouch
- ramp traversal
- stairs
- wall/obstacle collision
- landing

### Manual Xbox Series

If hardware is available:

- left stick analog movement
- right stick look
- A jump
- B crouch
- left-stick press sprint
- no drift

### Manual Switch Pro

If hardware is available:

- left stick analog movement
- right stick look
- physical south button B jumps
- physical east button A crouches
- left-stick press sprint
- no drift

If controller hardware is unavailable in Seele's environment, explicitly say so and validate the generic `<Gamepad>` bindings structurally. Do not claim hardware tests that did not happen.

## Seele Preview / WebGL

Do not break Seele Preview.

If Preview cannot expose native controller input, do not distort the desktop architecture to work around it.

Keep the Seele bridge/runtime required by repository hygiene.

## Out of Scope

Do not implement:

- final first-person camera
- third-person camera
- perspective switching
- final character art/animations
- weapons/shooting/aiming/damage
- enemy AI
- interaction gameplay
- inventory UI
- quests/reputation
- save-system expansion
- EVA/zero-G/magnetic boots
- ships/vehicles
- multiplayer
- final settings/accessibility UI
- PlayStation-specific support
- native Nintendo Switch build/export

Native Nintendo Switch deployment is a separate future platform milestone requiring Nintendo-approved tooling/SDK access.

## Definition of Done

Task 002 is complete only when:

1. branch starts from approved `main`
2. reusable player prefab exists
3. walk/sprint/jump/crouch work
4. grounding, ramps, stairs, and collision are reliable
5. mouse look works
6. gamepad right-stick look works
7. analog magnitude is preserved
8. keyboard/mouse and gamepad switch at runtime
9. Xbox Series physical layout is supported via Input System
10. Switch Pro physical layout is supported via Input System
11. dead zones prevent normal drift
12. gameplay code has no vendor-specific polling
13. no Task 003+ systems were added
14. Task 001 behavior remains valid
15. Unity compiles without blocking errors
16. `FoundationTest` runs without blocking console errors
17. tests pass
18. work is committed/pushed to `feature/player-controller`
19. Seele stops and reports completion

## Required Completion Report

Report:

### Engine / Packages
Unity, URP, and Input System versions.

### Branch / Commit
Branch, final SHA, `git status`, and remote-branch status.

### Implemented
Locomotion/input architecture.

### Files Changed
Exact files.

### Player Prefab
Path and component structure.

### Movement Tuning
Walk, sprint, crouch, acceleration, deceleration, jump height, gravity.

### Look Tuning
Mouse sensitivity, controller sensitivity, pitch clamp, dead-zone settings.

### Input Devices
Separate structural support from actual hardware testing for keyboard/mouse, Xbox Series, and Switch Pro.

### Runtime Device Switching
How it was validated.

### Tests
Automated/manual tests performed.

### Known Limitations
Deferred work.

### Risks / Technical Debt
Anything to review before merge.

### Recommended Next Task
Recommend but do not begin it.

Expected next milestone:

`Task 003 — First-Person Camera Foundation`

## Final Scope Reminder

Task 002 succeeds when movement feels reliable and the input architecture is future-proof. It does not succeed by adding more features.
