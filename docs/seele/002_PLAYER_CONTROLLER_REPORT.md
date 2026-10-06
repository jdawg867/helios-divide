# Task 002 — Player Controller Implementation and Validation

## Scope and source control

- Unity 6000.0.49f1, URP 17.0.4, Input System 1.14.0.
- Implemented on `feature/player-controller`, based on latest approved `main` at `67a472c136e9136b8c3e523cf9b2b0cb6b3d6aa0`.
- No Task 003 systems, final camera, perspective switching, combat, interaction gameplay, inventory, EVA, or character art/animation.
- No new package dependencies. Seele/native bridge, UniTask, and the foundation documentation are preserved.

## Architecture

Reusable prefab: `Assets/HeliosDivide/Player/HDPlayer.prefab`.

```text
HDPlayer
  CharacterController
  HDActiveDevice
  HDPlayerInput
  HDGroundedState
  HDPlayerLocomotion
  HDDevelopmentLook
  CameraPivot
    DevelopmentCamera (Camera + AudioListener)
```

The root is the one authoritative player and has a feet-level origin with unit scale. It has no dependency on FoundationTest. HDPlayerInput clones the existing HeliosDivideInput asset, enables only the five locomotion/look actions, and consumes logical actions. All 14 source actions and their GUIDs/bindings remain unchanged. No old Input Manager API or vendor-specific gameplay polling is used.

HDActiveDevice exposes `KeyboardMouse` / `Gamepad` from meaningful performed actions. No device pairing or control-scheme mask prevents switching between devices in one session. Look rate/delta interpretation uses the device driving the Look action, not the last unrelated movement button.

## Movement tuning

| Setting | Value |
|---|---:|
| Walk | 4.5 m/s |
| Sprint (hold) | 7.0 m/s |
| Crouch (hold Ctrl, C, or physical east button) | 2.5 m/s |
| Acceleration | 22 m/s² |
| Deceleration | 26 m/s² |
| Jump height | 1.2 m |
| Gravity | -20 m/s² |
| Grounded vertical speed | -2 m/s |
| Terminal fall speed | 40 m/s |
| Standing / crouching height | 1.8 / 1.1 m |
| Capsule radius / skin | 0.30 / 0.04 m |
| Step offset / slope limit | 0.55 m / 50° |
| Ground probe distance | 0.10 m |

Vector2.ClampMagnitude preserves analog movement below full input and caps diagonal keyboard movement. Sprint requires movement magnitude greater than 0.1 and is disabled while crouching. Jump requires grounded state and standing posture; there is no queued landing jump or double jump. Gravity is bounded while grounded. A slope-filtered foot spherecast supplements CharacterController grounding. Short controller substeps (at most 0.02 s, at most 0.10 s simulated per frame) protect collision at low frame rates.

Crouch keeps the feet fixed, reduces capsule height and speed, and changes eye height. Standing checks a non-trigger capsule volume and excludes the player itself. When blocked, releasing crouch leaves the player crouched until clearance is available. An overlap-buffer overflow conservatively blocks standing.

## Development look and dead zones

- Yaw rotates the root; pitch rotates CameraPivot and clamps to ±85°.
- Mouse: 0.12 degrees per delta unit, with no deltaTime multiplier.
- Right stick: 150 degrees/second at full processed input, multiplied by elapsed time.
- Eye heights: 1.62 m standing, 0.92 m crouching.
- Cursor capture/hiding occurs on enable and regained application focus; disabling the component restores previous cursor state.
- `HDInputSettings.asset` persists Input System dead-zone minimum 0.15 and maximum 0.95. The built-in Gamepad stickDeadzone processor is applied once at control level; no duplicate binding-level processor. Analog range beyond the inner dead zone is remapped normally.
- Input uses dynamic updates. Desktop focus behavior is retained; test-only focus overrides use a temporary settings clone and are restored afterward.

## Devices and physical layout

| Action | Generic path | Xbox Series | Switch Pro |
|---|---|---|---|
| Move / look | leftStick / rightStick | Sticks | Sticks |
| Jump | buttonSouth | A | B |
| Crouch | buttonEast | B | A |
| Sprint | leftStickPress | LS press | LS press |
| Interact (future) | buttonWest | X | Y |
| Reload (future) | buttonNorth | Y | X |
| Aim / fire (future) | leftTrigger / rightTrigger | LT / RT | ZL / ZR |
| Previous / next weapon (future) | leftShoulder / rightShoulder | LB / RB | L / R |
| Perspective / inventory (future) | dpad/down / dpad/up | D-pad | D-pad |
| Pause (future) | start | Menu | Plus |

Hardware testing was not performed: no native gamepads were exposed in the environment. Xbox and Switch support is structural through Unity's generic Gamepad layouts and physical-position bindings, not a claim of actual hardware testing or native Switch deployment. Keyboard/mouse and gamepad behavior were tested with synthetic Input System devices in the real Unity runtime.

## Foundation integration and geometry corrections

FoundationTest contains one linked HDPlayer prefab instance at PlayerSpawn's X/Z and yaw, initially 0.23 m above the floor. It settles safely. The old camera is disabled; the spawn marker collider is a trigger so it cannot become a standing surface. Environment materials, dimensions, obstacles, floor, perimeter walls, lighting, platform, and bootstrap are preserved.

Two existing traversal defects were corrected using Unity editor APIs:

1. Ramp Z rotation changed from -18.4° to +18.4° so it rises toward the platform rather than presenting its high end on the wrong side.
2. The four 0.5 m risers retain their sizes/heights but move to Z = 0.7 / 1.7 / 2.7 / 3.7. Previously, the platform wall overlapped the upper treads; a runtime test stalled at Z≈3.614. Repositioning produces usable treads and a top tread that meets the platform.

## Validation actually performed

- Unity project readiness and compilation succeeded with no compile errors.
- Unity Test Runner completed the `HeliosDivide.Tests.Editor` assembly: **7 tests passed, 0 failed, 0 skipped**. This includes both existing FoundationSmokeTest tests, four player structural tests, and the runtime contract coroutine that enters and exits Play Mode.
- Runtime/editor/test assembly definitions isolate player code and make all tests discoverable without adding package dependencies.
- `PlayerRuntimeTests.RunInPlayMode()` executed against the real scene CharacterController and action reader. It injects virtual Keyboard, Mouse, and Gamepad states and calls the same Tick methods used by Update.
- All 14 runtime assertion groups passed: spawn stability; WASD/diagonal cap; sprint hold/release/no idle sprint; analog movement/gamepad sprint/keyboard return; deadzones; jump/no double jump/south button/landing; Ctrl/C/east crouch and blocked standing; mouse delta vs stick rate/pitch clamp/device switching; wall collision; obstacle collision; ramp ascent; ramp descent; stairs ascent; stairs descent/landing.
- Measured stable feet Y≈0.04 m, diagonal speed 4.5 m/s, jump apex≈1.132 m, ramp/platform feet Y≈2.04 m, stair/platform feet Y≈2.04 m. The 1.2 m jump setting is subject to discrete integration tolerance.
- A separate natural-frame async Play Mode check left the normal Update path enabled: virtual W moved 3.61 m, crouch eye height became 0.92 m, release restored 1.62 m, and the player remained grounded.
- Console checks after the final runtime passes reported zero error entries.
- A player-camera screenshot was inspected: gray-box environment renders without missing/magenta materials.

The Unity Test Runner coroutine uses the same runtime harness as the direct Play Mode checks; these are automated synthetic-device tests, not human physical-hardware testing.

## Seele Preview / WebGL validation

- Final publication succeeded with all four matching WebGL artifacts (loader, framework, WASM, data), and the game workspace manifest is ready.
- The actual published build was loaded in Chromium using an ignored local smoke-check page. It reached Ready, rendered FoundationTest, loaded all four published resources, and responded to a 1.5-second synthetic forward-key input. Before/after camera images confirmed movement.
- No uncaught browser page errors or gameplay exceptions were reported during that smoke check.
- WebGL logs do contain non-blocking warnings: scene identifiers from the locally installed, gitignored MCP package are absent from the exported runtime; two hidden URP utility/debug shaders are unsupported on the verification GPU; and a light-cookie format falls back. The missing identifier GUID was traced to `MCPForUnity.Runtime.UniqueIdentifier`, not a player gameplay component. Rendering and movement still worked. These warnings are recorded for review rather than adding a dependency or changing the preserved foundation to suppress them.
- Open and play from the frontend Preview. Native controller exposure in that browser environment remains unverified.

## How to test

1. Open the repository in Unity 6000.0.49f1 and allow package resolution.
2. Open FoundationTest or enter Play Mode using the configured startup scene.
3. Focus the Game view. WASD moves, mouse looks, Space jumps, hold Shift sprints, and hold Ctrl or C crouches.
4. Walk both approaches to the raised platform, collide with walls/obstacles, and jump/land.
5. Connect a supported Xbox Series or Switch Pro controller. Check gradual left-stick input, right-stick look, south/east buttons, and left-stick press sprint.
6. In the same session alternate WASD/stick and mouse/right-stick without reloading. Verify no drift and release sprint/crouch.
7. Use Test Runner Edit Mode for FoundationSmokeTest, PlayerControllerTests, and PlayerRuntimeTests. The runtime contract uses temporary virtual devices and removes them/restores settings in a finally block.

## Known limitations and review risks

- Actual Xbox Series/Switch Pro hardware, Bluetooth/HID drivers, browser-native gamepad exposure, and subjective movement feel require human verification before merge.
- No final camera settings, head bob, shake, recoil, stamina, custom gravity, mantling, vaulting, pause menu, rebinding UI, or character model/animation.
- The development view is intentionally plain. The unusually tall gray-box risers require the exposed 0.55 m step offset; revisit this for production level metrics.
- Per-frame simulation caps at 0.10 s; at extreme stalls below 10 FPS, movement intentionally slows rather than attempting an unbounded collision step.
- Local Seele package resolution may rewrite packages-lock.json with ignored MCP/draco/gltfast entries. Those are not intended Task 002 changes and must not be committed.

## Exact files changed

New files (each Unity asset also includes the matching `.meta` file):

- `Assets/HeliosDivide/Core/HDInputSettings.asset`
- `Assets/HeliosDivide/HeliosDivide.Runtime.asmdef`
- `Assets/HeliosDivide/Editor/HeliosDivide.Editor.asmdef`
- `Assets/HeliosDivide/Player/HDActiveDevice.cs`
- `Assets/HeliosDivide/Player/HDDevelopmentLook.cs`
- `Assets/HeliosDivide/Player/HDGroundedState.cs`
- `Assets/HeliosDivide/Player/HDPlayer.prefab`
- `Assets/HeliosDivide/Player/HDPlayerInput.cs`
- `Assets/HeliosDivide/Player/HDPlayerLocomotion.cs`
- `Assets/HeliosDivide/Tests/Editor/HeliosDivide.Tests.Editor.asmdef`
- `Assets/HeliosDivide/Tests/Editor/PlayerControllerTests.cs`
- `Assets/HeliosDivide/Tests/Editor/PlayerRuntimeTests.cs`
- `docs/seele/002_PLAYER_CONTROLLER_REPORT.md`

Modified:

- `Assets/HeliosDivide/World/FoundationTest.unity`
- `ProjectSettings/EditorBuildSettings.asset`

The original input-action asset, foundation scripts/tests, package manifest/committed lockfile, and repository hygiene documentation are unchanged. Unity-generated serialization whitespace is retained rather than hand-editing serialized assets.

## Next milestone

Recommend **Task 003 — First-Person Camera Foundation** only after Task 002 review. Task 003 is not started.
