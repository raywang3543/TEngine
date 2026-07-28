# Repository Guidelines

## Project Structure & Module Organization

`UnityProject/` is the runnable Unity project (currently pinned to Unity `6000.3.20f1`). Framework code lives under `Assets/TEngine/{Runtime,Editor}`, startup code under `Assets/Launcher`, and hot-update assemblies under `Assets/GameScripts/HotFix/{GameLogic,GameProto}`. Game assets are split between editable sources in `Assets/AssetRaw` and bundle-oriented content in `Assets/AssetArt`. Treat `Packages/YooAsset`, `Packages/UniTask`, and other vendored packages as upstream code unless the change explicitly targets them.

Configuration sources are in `Configs/GameConfig/Datas`; generated C# and binary outputs go to `GameProto/GameConfig` and `Assets/AssetRaw/Configs/bytes`. Documentation and screenshots live in `Books/`. Build automation is in `BuildCLI/`, while standalone Roslyn tooling is under `Tools/GameEventSourceGenerator/`.

## Build, Test, and Development Commands

- Open `UnityProject/` with Unity Hub using the version in `ProjectSettings/ProjectVersion.txt`; run the `Assets/Scenes/main.unity` launcher scene for local validation.
- `cd Configs/GameConfig && ./gen_code_bin_to_project.sh` regenerates Luban client code and binary tables. Commit source data and generated outputs together.
- `dotnet build Tools/GameEventSourceGenerator/SourceGenerator.sln` compiles the event analyzer/source-generator tools.
- Configure local paths in `BuildCLI/path_define.sh`, then run `./BuildCLI/build_android.sh` for the batch Android build.
- Run tests from **Window > General > Test Runner**. For CI, use Unity’s `-batchmode -projectPath UnityProject -runTests -testPlatform EditMode -testResults results.xml -quit` arguments.

## Coding Style & Naming Conventions

Use four-space indentation and Allman braces for C#. Keep lines within the ReSharper limit of 180 characters from `UnityProject/.editorconfig`. Use `PascalCase` for types and public members, `_camelCase` for private fields, and `UPPER_SNAKE_CASE` for established constants. Match the surrounding namespace and XML documentation style. Never hand-edit generated Luban files. Add and move Unity assets together with their `.meta` files.

## Testing Guidelines

The repository has Unity Test Framework support but no substantial first-party test suite yet. Add EditMode or PlayMode test assemblies near the feature, name fixtures and files `*Tests`, and use `[Test]` or `[UnityTest]` as appropriate. Before submitting, run relevant tests plus an Editor play-through; asset or UI changes should also verify bundle loading.

## Commit & Pull Request Guidelines

Recent history mixes terse `update` commits with focused Chinese or English summaries; prefer the focused form, such as `Fix RawFile handle cast crash`. Keep commits scoped and include regenerated artifacts when required. Pull requests should explain behavior and validation, link related issues, and attach screenshots or recordings for UI/editor changes. Note tested Unity version and target platform for build-sensitive changes.
