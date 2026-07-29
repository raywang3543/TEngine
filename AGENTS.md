# Repository Guidelines

## Project Structure & Documentation

`UnityProject/` is the runnable Unity `6000.3.20f1` project. Framework runtime and editor code live in
`Assets/TEngine/{Runtime,Editor}`; native startup and procedure code lives in `Assets/Launcher` and
`Assets/GameScripts/Procedure`. Hot-update business code belongs in `Assets/GameScripts/HotFix/GameLogic`, while
Luban output belongs in `HotFix/GameProto`. Editable assets are under `Assets/AssetRaw`; bundle-oriented assets are
under `Assets/AssetArt`.

Place the game's main logic in `Assets/GameScripts/HotFix/GameLogic/Core/`. Organize supporting UI, module, or
feature-specific code in neighboring `GameLogic` subdirectories rather than mixing it into framework runtime code.

Use `Books/` for task-focused usage guides and `UnityProject/repowiki/zh/content/` for deeper architecture/API
reference. Some pages describe older Unity versions or historical APIs, so current source, assembly definitions,
settings assets, and `ProjectVersion.txt` take precedence.

## Framework Usage Rules

- Application code accesses services through `GameModule.Resource`, `.UI`, `.UIToolkit`, `.Audio`, `.Scene`,
  `.Timer`, `.Fsm`, `.Procedure`, or `.Localization`. Reserve `ModuleSystem.GetModule<T>()` for framework internals
  and custom module implementation.
- Prefer UniTask for I/O and lengthy work. Await async APIs; when a procedure lifecycle method starts a
  `UniTaskVoid`, call `.Forget()`. Do not introduce `async void` or new coroutine-based flows.
- Load GameObjects with `LoadGameObjectAsync` so `AssetsReference` tracks ownership. Addressable locations omit file
  extensions and must be unique. Retain dynamically loaded non-GameObject assets and pass them to `UnloadAsset`.
- Define cross-module events with `[EventInterface]`; use generated event IDs rather than hard-coded integers.
  `GameEventHelper.Init()` must remain first in `GameApp.Entrance`. UI classes register through `AddUIEvent`; non-UI
  listeners must unregister symmetrically or clear a local `GameEventMgr`.
- Implement screens as `[Window]`-annotated `UIWindow` classes and reusable parts as `UIWidget`. Use ScriptGenerator
  prefixes such as `m_btn_`, `m_img_`, `m_tmp_`, and `m_item_`. Do not override `OnUpdate` with an empty method.
- Keep each `ProcedureBase` state single-purpose and transition with `ChangeState<T>()`. Pooled data implements
  `IMemory.Clear`; never access or release it twice after `MemoryPool.Release`. Pooled entities inherit `ObjectBase`
  and clear references in `Release`.
- Edit Luban sources in `Configs/GameConfig/Datas`, then regenerate code and binary data. Never hand-edit generated
  files; lazy configuration access through `ConfigSystem.Instance.Tables` is preferred.

## Build, Test, and Development Commands

- Open `UnityProject/` with the version in `ProjectSettings/ProjectVersion.txt`; validate from `Assets/Scenes/main.unity`.
- `cd Configs/GameConfig && ./gen_code_bin_to_project.sh` regenerates client configuration.
- `dotnet build Tools/GameEventSourceGenerator/SourceGenerator.sln` builds event tooling.
- Configure `BuildCLI/path_define.sh`, then run `./BuildCLI/build_android.sh` for Android.
- Run EditMode/PlayMode tests through Unity Test Runner, or use
  `-batchmode -projectPath UnityProject -runTests -testPlatform EditMode -testResults results.xml`.

## Coding Style & Naming

Use four spaces, Allman braces, and blank lines between logical sections. Aim for 120-character lines; never exceed
the `.editorconfig` ReSharper limit of 180. Use `PascalCase` for types, methods, properties, and constants,
`camelCase` for locals/parameters, and `_camelCase` for private fields. Keep namespaces aligned with assemblies
(`TEngine`, `GameLogic`, `Launcher`) and directories. Add XML summaries to public APIs and comments only for intent
or edge cases. Move Unity assets with their `.meta` files.

## Testing, Commits & Pull Requests

Name test fixtures/files `*Tests` and place EditMode or PlayMode assemblies near the feature. Run relevant tests plus
an Editor play-through; verify bundle loading for asset/UI changes. Prefer focused commit subjects such as
`Fix RawFile handle cast crash` over `update`. Pull requests should describe behavior, validation, tested Unity
version/platform, linked issues, and include screenshots or recordings for UI/editor changes.
