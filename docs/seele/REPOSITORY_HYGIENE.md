# Repository Hygiene

Helios Divide keeps a small Unity 6 baseline plus the Seele editor/runtime files needed to continue development. Template content that the game does not use is removed.

## Must remain — Unity project open/build

- `ProjectSettings/`
- `Packages/manifest.json` and `Packages/packages-lock.json`
- URP settings under `Assets/Settings/` except the unused Sample Scene volume profile
- `com.unity.inputsystem`, `com.unity.render-pipelines.universal`, `com.unity.ugui`, `com.unity.test-framework`
- IDE packages `com.unity.ide.visualstudio` and `com.unity.ide.rider`

## Must remain — Seele editor / preview / WebGL

- `Packages/com.seele.native-bridge/` — Seele native/WebGL bridge used by the editor and published player
- `Packages/UniTask/` (`com.cysharp.unitask`) — required dependency of `com.seele.native-bridge` (`NativeBridge.asmdef` references UniTask; package.json depends on `com.cysharp.unitask` 2.5.10). Helios Divide gameplay does not call UniTask directly.
- `Assets/Plugins/WebJs.jslib` — Seele WebGL JavaScript callbacks (`IsInSeele`, `unityCallback`)
- `Assets/Seele/` — Seele runtime/editor helpers (event bus, input simulation, probes, default animation stubs)
- `Assets/Scripts/GameEventTypes.cs` — Seele `EventDispatcher` event contract; do not delete or renumber events
- `Assets/SeeleGameData/` — Seele project metadata
- `Assets/UI Toolkit/` — Seele default panel settings / CJK USS
- `Assets/TextMesh Pro/` — URP UI/CJK font pipeline used by the Seele base project
- `Assets/Scenes/scene_config` — Seele scene pointer; currently `FoundationTest`

`Packages/seele-unity-mcp/` is installed locally by Seele Unity editor tooling and is gitignored. The committed `packages-lock.json` omits that embedded package so a fresh Unity 6000.0.49f1 checkout can open without a missing-package error. Local Seele editor sessions may re-add it to the lockfile while the ignored folder is present; do not commit that local-only entry.

## Removed — unused Unity template content

- `Assets/Scenes/SampleScene.unity` — replaced by `FoundationTest`
- `Assets/Settings/SampleSceneProfile.asset` — only used by SampleScene
- Template registry packages not referenced by Helios Divide or Seele runtime: AI Navigation, Collab Proxy, Timeline, Visual Scripting, Multiplayer Center
