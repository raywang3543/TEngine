# Repository Guidelines

## Project Structure & Documentation

`UnityProject/` is the runnable Unity `6000.3.20f1` project. Framework runtime and editor code live in
`Assets/TEngine/{Runtime,Editor}`; native startup and procedure code lives in `Assets/Launcher` and
`Assets/GameScripts/Procedure`. Hot-update business code belongs in `Assets/GameScripts/HotFix/GameLogic`, while
Luban output belongs in `HotFix/GameProto`. Editable assets are under `Assets/AssetRaw`; bundle-oriented assets are
under `Assets/AssetArt`.

Place all game-specific logic in `Assets/GameScripts/HotFix/GameLogic/Core/`, including rules, runtime state,
models, controllers, content catalogs, scoring, progression, shops, and feature behavior. Code outside `Core/`
must not implement concrete game rules or mutate game state directly; neighboring `GameLogic` directories are only
for reusable extension components, framework adapters, UI presentation/binding, resource or platform integration,
and generic tool/helper methods. UI and integration layers must forward commands to `Core/` controllers and render
the state exposed by `Core/` models. All bidirectional communication across the `Core/` boundary must use the
explicit event IDs declared in `GameLogic/EventDefine.cs` and pass through `CarnivalSystem`; do not define these
events with `[EventInterface]` or generated event ID classes. External code must never retain a Ctrl or Model
reference. Register, unregister, and send cross-boundary events directly with `GameEvent.AddEventListener`,
`GameEvent.RemoveEventListener`, and `GameEvent.Send`, keeping registration and removal symmetric.
`CarnivalSystem` receives command events, delegates them to the appropriate Ctrl/Model, and publishes read-only
state events back to presentation and integration layers.

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

## Unity 6.3 UI Toolkit Rules

- Prefer declarative UXML/USS for visual effects. Assign imported materials with
  `-unity-material: url("project://database/Assets/...mat")`; use
  `VisualElement.style.unityMaterial` or `MaterialDefinition` only for runtime-selected materials or per-element
  parameter overrides.
- Use USS `filter` for subtree post-processing. Built-ins include `blur`, `grayscale`, `invert`, `opacity`, `sepia`,
  `tint`, `hue-rotate`, and `contrast`; combine passes in one declaration because separate rules override rather
  than merge. Match function type and order between states when animating `filter`.
- Treat materials and filters differently: materials shade UI meshes directly, while filters render an element and
  its descendants to an intermediate texture. Avoid large, nested, multi-pass, or continuously animated filters.
- Do not recreate UI effects with `RenderTexture` plus `Graphics.Blit` when these APIs suffice. Keep static effects
  in USS and reserve controllers for game state, interaction, dynamic class changes, and resource lifetime.
- This project currently uses the Built-in Render Pipeline. UI Shader Graph's UI target requires URP; use a
  UI Toolkit-compatible shader such as `Assets/AssetRaw/Shaders/CarnivalUIEffects.shader`, or migrate pipelines only
  with explicit approval. Package UXML, USS, materials, shaders, images, and fonts together and retain their
  YooAsset handles while the UI is open.

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
