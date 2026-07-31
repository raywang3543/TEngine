# AGENTS.md

本文件面向 AI 编码代理，描述本仓库的结构、技术栈、开发规范与常用命令。阅读本文件前无需了解项目的任何背景。

## 项目概述

本仓库是 **TEngine**（Unity 客户端框架解决方案）的一个分支，用于在框架之上开发原创游戏。`GameLogic/Core/` 当前为空，游戏逻辑待按「Core/ 边界约定」重建。

- Unity 版本：**6000.3.20f1**（Unity 6.3，见 `UnityProject/ProjectSettings/ProjectVersion.txt`）。
- 渲染管线：**Built-in Render Pipeline**（`GraphicsSettings.asset` 中 `m_CustomRenderPipeline` 为空），线性色彩空间。
- 目标平台：Windows / macOS / Android / iOS / WebGL（构建脚本目前仅覆盖 Android）。
- 注意：根目录 `README.md` 与 `Books/` 描述的是上游 TEngine 框架（徽章标注 Unity 2021 等旧信息），与当前工程不一致时，**以当前源码、程序集定义、`ProjectVersion.txt` 和设置资产为准**。

## 仓库结构

```
TEngine/
├── UnityProject/          # 可运行的 Unity 工程（唯一的 Unity 项目）
├── Configs/GameConfig/    # Luban 配置表工程（Excel 数据源 + 生成脚本）
├── Tools/                 # Luban.dll、GameEventSourceGenerator（事件 ID 源生成器）、FileServer
├── BuildCLI/              # 命令行出包脚本（当前仅 Android）
├── Books/                 # 框架使用文档（中文，任务导向）
├── work-log/              # 每日工作日志（raw-YYYY-MM-DD.md）
└── outputs/
```

### UnityProject/Assets 关键目录

```
Assets/
├── TEngine/               # 框架本体（Runtime/ 运行时模块、Editor/ 编辑器扩展、Settings/）
├── Launcher/              # 原生启动层程序集（启动 UI、更新提示，Launcher.asmdef）
├── GameScripts/
│   ├── GameEntry.cs       # 主程序集入口（MonoBehaviour，初始化模块并启动流程）
│   ├── Procedure/         # 主程序集的启动流程状态（ProcedureLaunch → ... → ProcedureStartGame）
│   └── HotFix/
│       ├── GameProto/     # 热更程序集：Luban 生成代码（GameConfig/）、协议（GameProtocol/）
│       └── GameLogic/     # 热更程序集：全部游戏业务代码（见下）
├── AssetRaw/              # 热更资源源文件（UI、UIToolkit、Audios、Configs、DLL、Fonts、Shaders、Scenes 等）
├── AssetArt/              # 打包美术资源（Atlas 图集）
├── Editor/                # 工程级编辑器工具（UIScriptGenerator、AssetBundleCollector、AtlasRefWindow 等）
├── Scenes/                # 主场景 main.unity
└── HybridCLRGenerate/     # HybridCLR 生成产物（勿手改）
```

### GameLogic 程序集内部结构

```
GameLogic/
├── GameApp.cs             # 热更域入口 GameApp.Entrance
├── GameModule.cs          # 模块访问门面（GameModule.Resource/.UI/.UIToolkit/...）
├── EventDefine.cs         # 跨模块事件 ID（RuntimeId.ToRuntimeId 字符串转整型）
├── SingletonSystem/       # 单例系统（Singleton/SingletonBehaviour/SingletonSystem）
├── Core/                  # 游戏核心规则（唯一允许实现具体游戏规则的地方；当前为空，待重建）
├── Module/                # 自定义 UIModule（uGUI：UIWindow/UIWidget/UIBase）与 UIToolkitModule
├── UI/                    # uGUI 界面（LoginUI、BattleMainUI）
├── UIToolkit/             # UI Toolkit 控制器（WelcomeScreenController）
├── Tool/                  # 通用工具
```

## 技术栈与运行时架构

- **热更新：HybridCLR**。所有平台均定义了 `ENABLE_HYBRIDCLR`；热更程序集为 **GameProto + GameLogic**（见 `ProjectSettings/HybridCLRSettings.asset`），编译产物在 `HybridCLRData/HotUpdateDlls`。AOT 层（Launcher、Procedure、TEngine、主程序集）不得直接引用热更类型，热更代码经反射调用 `GameApp.Entrance` 进入。
- **资源管理：YooAsset**（本地包 `Packages/YooAsset`）。支持 EditorSimulateMode / OfflinePlayMode / HostPlayMode，AssetReference 自动生命周期，LRU/ARC 缓存。收集器配置在 `Assets/Editor/AssetBundleCollector`。
- **配置表：Luban**。Excel 数据源在 `Configs/GameConfig/Datas`（含 `__tables__.xlsx` 注册表，当前业务表为 `item.xlsx`），生成 C# 代码到 `GameProto/GameConfig/`、二进制到 `AssetRaw/Configs/bytes/`。运行时通过 `ConfigSystem.Instance.Tables` 懒加载访问。
- **异步：UniTask**（本地包 `Packages/UniTask`）。另有 `Packages/Protobuf`、NuGet Newtonsoft.Json。
- **模块系统**：框架模块在 `Assets/TEngine/Runtime/Module/`（Resource、Audio、Fsm、Procedure、Scene、Timer、Localization、ObjectPool、Debuger 等），核心设施在 `Runtime/Core/`（GameEvent、MemoryPool、Log、Utility、ModuleSystem）。
- **启动链路**：`GameEntry.Awake` → 初始化 UpdateDriver/Resource/Fsm 等模块 → `Settings.ProcedureSetting.StartProcedure()` → 流程状态机：`ProcedureLaunch → ProcedureSplash → ProcedureInitPackage → ProcedurePreload → ProcedureInitResources → ProcedureCreateDownloader → ProcedureDownloadFile → ProcedureDownloadOver → ProcedureClearCache → ProcedureLoadAssembly → ProcedureStartGame`（本分支已无 UpdateVersion/UpdateManifest 状态，以 `Assets/GameScripts/Procedure/` 实际文件为准）→ 反射进入热更域 `GameApp.Entrance`。
- **游戏入口**：`GameApp.Entrance` 经 `StartGameLogic()` 启动游戏逻辑（当前 `StartGameLogic` 为空，待新游戏逻辑接入）。
- **游戏架构（MVE 变体）**：`Core/` 内由一个统一入口系统监听 `EventDefine` 中的命令事件，转发给 Ctrl 与内容模型（Model），并把只读状态以事件发布回表现层。表现层（uGUI 窗口、UI Toolkit 控制器）只发送命令、渲染状态，不得持有 Ctrl/Model 引用。数据优先由 Luban 配置表驱动，经内容模型转换为运行时模型（生成代码不直接进入 Core 业务边界）。

## 构建、生成与测试命令

```bash
# 重新生成 Luban 配置代码与二进制（改表后必跑；另有 *_lazyload 变体脚本）
cd Configs/GameConfig && ./gen_code_bin_to_project.sh

# 构建事件 ID 源生成器工具
dotnet build Tools/GameEventSourceGenerator/SourceGenerator.sln

# Android 命令行出包（先按本机环境改 BuildCLI/path_define.sh）
./BuildCLI/build_android.sh   # 调用 TEngine.ReleaseTools.AutomationBuildAndroid

# EditMode 测试（命令行方式）
<Unity>/Unity -batchmode -projectPath UnityProject -runTests \
  -testPlatform EditMode -testResults results.xml -quit
```

日常开发：用 **Unity 6000.3.20f1** 打开 `UnityProject/`，从 `Assets/Scenes/main.unity` 运行；测试也可在 Editor 的 Test Runner（EditMode）中执行。热更出包流程见 `README.md` 与 `Books/1-快速开始.md`（HybridCLR Generate/Build → YooAsset AssetBundle Builder → Build And Run）。

## 框架使用规范

- 业务代码通过 `GameModule.Resource`、`.UI`、`.UIToolkit`、`.Audio`、`.Scene`、`.Timer`、`.Fsm`、`.Procedure`、`.Localization` 访问模块；`ModuleSystem.GetModule<T>()` 保留给框架内部和自定义模块实现。
- I/O 与耗时操作优先 UniTask；异步 API 要 await；流程生命周期方法里启动的 `UniTaskVoid` 必须 `.Forget()`。**不要**引入 `async void` 或新的协程流程。
- 加载 GameObject 用 `LoadGameObjectAsync`，让 `AssetsReference` 追踪所有权；Addressable 定位名不带扩展名且必须唯一；动态加载的非 GameObject 资源要持有引用并传给 `UnloadAsset` 释放。
- 事件：
  - 游戏跨模块/跨 `Core/` 边界的事件一律定义在 `GameLogic/EventDefine.cs`，用 `RuntimeId.ToRuntimeId("EventDefine.Xxx")` 生成整型 ID，**不要**用 `[EventInterface]` 或源生成事件类。
  - 跨边界通信统一 `GameEvent.AddEventListener` / `RemoveEventListener` / `Send`，注册与注销必须对称。
  - uGUI 界面内用 `UIBase.AddUIEvent(...)`（窗口关闭时自动清理）；非 UI 监听者必须对称注销或清理局部 `GameEventMgr`。
- UI（uGUI）：界面实现为带 `[Window(UILayer.Xxx)]` 的 `UIWindow`，可复用部件为 `UIWidget`；代码生成器（`Assets/Editor/UIScriptGenerator`）绑定前缀 `m_btn_`、`m_img_`、`m_tmp_`、`m_item_` 等；不要覆写空的 `OnUpdate`。
- 流程：每个 `ProcedureBase` 状态职责单一，用 `ChangeState<T>()` 切换。
- 池化：内存池数据实现 `IMemory.Clear`；`MemoryPool.Release` 后不得再次访问或重复释放；池化实体继承 `ObjectBase` 并在 `Release` 中清空引用。
- 配置表：只改 `Configs/GameConfig/Datas` 里的 Excel，再跑生成脚本；**绝不手改生成文件**（`GameProto/GameConfig/`、`AssetRaw/Configs/bytes/`、`HybridCLRGenerate/`）。

## Core/ 边界约定（游戏专属）

- 所有游戏专属逻辑——规则、运行时状态、Model、Ctrl、内容目录、计分、进程、商店、玩法行为——只能放在 `Assets/GameScripts/HotFix/GameLogic/Core/`。
- `GameLogic` 的其它相邻目录（`UI/`、`UIToolkit/`、`Module/`、`Tool/`）只放可复用扩展组件、框架适配器、UI 表现/绑定、资源或平台集成、通用工具；不得实现具体游戏规则或直接改游戏状态。
- UI 与集成层通过事件把命令转发给 `Core/`（经 `CoreSystem`），只渲染 `Core/` 模型暴露的只读状态；外部代码**绝不保留** Ctrl 或 Model 引用。
- 卡牌数值改动优先走 Luban 配置表（见上）。

## UI Toolkit 规范（Unity 6.3 / Built-in RP）

- 视觉效果优先用声明式 UXML/USS（布局与样式在 `Assets/AssetRaw/UIToolkit/Layout|Style`，控制器在 `GameLogic/UIToolkit/`，经 `UIToolkitModule` + `UITypes` 打开）。
- 材质用 `-unity-material: url("project://database/Assets/...mat")` 在 USS 中指定；`VisualElement.style.unityMaterial` / `MaterialDefinition` 仅用于运行时选材或逐元素参数覆盖。
- 子树后期处理用 USS `filter`（`blur`/`grayscale`/`invert`/`opacity`/`sepia`/`tint`/`hue-rotate`/`contrast`）；多个 filter 组合写在同一条声明里（分散规则会互相覆盖而非合并）；做 `filter` 动画时各状态的函数类型与顺序要一致。
- 材质与 filter 不同：材质直接给 UI 网格着色，filter 会把元素及子树渲染到中间纹理。避免大面积、嵌套、多 pass 或持续动画的 filter。
- UXML、USS、材质、shader、图片、字体要一起打包，UI 打开期间保留其 YooAsset 句柄。

## 代码风格

- 4 空格缩进、Allman 大括号、逻辑块之间空行；行宽目标 120 字符，不得超过 `.editorconfig` 的 ReSharper 上限 180。
- 类型/方法/属性/常量 `PascalCase`，局部变量与参数 `camelCase`，私有字段 `_camelCase`。
- 命名空间与程序集/目录对齐（`TEngine`、`GameLogic`、`Launcher`、`Procedure`）。
- 公共 API 写 XML 注释（项目注释为中文）；只为意图或边界情况写行内注释。
- 移动 Unity 资产必须连同 `.meta` 文件一起移动。

## 测试

- 当前无 EditMode 测试；重建 `Core/` 时应同步在 `UnityProject/Assets/Tests/EditMode/` 补回 NUnit 测试。
- `Core/` 的 Ctrl/Model 应保持纯 C#、支持注入随机种子，设计上便于 EditMode 直测——新增规则应同步补测试。
- 改动后运行相关 EditMode 测试并做一次 Editor 内实际运行；涉及资源/UI 的改动还要验证 bundle 加载链路。

## 安全与注意事项

- `BuildCLI/path_define.sh` 含有本机绝对路径（Unity 安装目录、工作区路径），属于个人环境配置，提交前注意不要泄露无关本机信息。
- 不要手改任何生成产物（Luban 生成代码/二进制、HybridCLRGenerate、HybridCLRData）。
- AOT 程序集（Launcher、Procedure、TEngine）不得引用热更程序集类型；热更侧也不要反向依赖启动层内部实现。
- 仓库文档（`README.md`、`Books/`、`UnityProject/repowiki/zh/content/`）部分描述旧版 Unity/历史 API，与源码冲突时以源码为准。

## 文档导航

- `Books/`：框架各模块使用指南（资源、事件、内存池、对象池、UI、配置表、流程、网络）。
- `UnityProject/repowiki/zh/content/`：更深的架构与 API 参考（项目概述、核心架构、事件系统、资源管理、UI 系统、热更新、部署发布等）。
- `README.md`：上游 TEngine 介绍与热更出包步骤。
- `work-log/`：历史开发记录，可从中了解近期改动与决策背景。
