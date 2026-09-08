# GameProject 游戏框架分析与开发计划

## 1. 总体判断

`GameProject` 已具备 Unity 游戏框架的基础设施，主要包括：

- `ManagerHub` 统一驱动生命周期和主循环。
- 协程、计时器、事件、FSM、场景等逻辑调度模块。
- Addressables 资源加载、缓存和引用计数。
- 分层 UI、界面栈、子界面、Widget 和 UI 队列。
- 本地化、音频、红点、日志和本地存档。
- `GameEngineEditor` Excel 辅助工具。
- `GameLogic` 当前主要包含本地化业务数据派生类。

当前框架更接近“基础模块集合”，尚未形成完整的游戏启动、配置、账号、网络、玩家数据和业务场景闭环。下一阶段不建议继续堆叠零散 Manager，应优先完成框架主链路。

## 2. 当前架构

```text
ManagerHub
    +-- CoroutineManager
    +-- TimerManager
    +-- EventManager
    +-- FsmManager
    +-- SceneManager
    +-- UIManager
    +-- UIWidgetManager
    +-- UIJumpManager
    +-- AssetManager
    +-- RedDotManager
    +-- AudioManager

GameEngine
    +-- GameNative
    +-- Unity / Addressables / TextMeshPro 等外部依赖

GameLogic
    +-- 当前主要是本地化业务数据

GameEngineEditor
    +-- Excel 工具
    +-- Inspector 工具
```

## 3. 重点分析

### 3.1 生命周期

核心入口为 `GameEngine/Runtime/Base/ManagerHub.cs`。其优点是统一初始化、Tick、Login 和 Logout，并对单个管理器的异常进行隔离。

主要问题：

- `ManagerHub` 同时承担底层生命周期和全部帧驱动，职责偏重。
- `ILogin` 实际承担初始化、会话清理和资源释放，语义偏窄。
- 缺少 `GameBootstrap` 和启动状态机。
- Tick 顺序硬编码，后续模块增加后维护成本会上升。

建议保留 `ManagerHub` 作为底层驱动器，在其上增加 `GameBootstrap` 和启动 FSM。

### 3.2 资源系统

`AssetManager` 已支持异步加载、请求合并、引用计数、延迟缓存释放和资源后端抽象，是目前相对完整的模块。

后续需要补充：

- `AssetHandle<T>`，避免调用方通过地址和类型手工释放。
- 加载状态：`Loading`、`Succeeded`、`Failed`、`Released`。
- 加载耗时、引用数、缓存数和未释放资源诊断。
- 加载取消和失败后的统一处理。

### 3.3 场景系统

`SceneManager` 当前是逻辑场景管理器，支持注册、切换、进入、退出、更新和销毁，但还没有与 Unity Scene 或 Addressables 场景加载形成异步闭环。

建议分层：

```text
GameSceneManager
    负责游戏逻辑场景和业务状态

UnitySceneLoader
    负责 Unity Scene / Addressables Scene 的加载卸载
```

目标流程：请求切换、退出旧场景、预加载资源、加载 Unity 场景、创建逻辑场景、进入目标场景、发布完成事件。

### 3.4 UI 系统

`UIManager` 已支持分层、窗口附属关系、子界面、Widget、界面队列、导航栈和生命周期，功能较完整。

主要风险是职责过重，当前同时负责实例创建、Prefab 加载、生命周期、层级、导航、父子关系、队列和显示刷新。

建议逐步拆分：

- `UIViewFactory`：实例创建和绑定。
- `UILayerController`：层级和显示隐藏。
- `UINavigationStack`：导航栈。
- `UIWindowOwnership`：窗口附属关系。
- `UIOpenQueue`：打开队列。

同时应尽早定义异步 UI 打开、加载失败和取消模型。

### 3.5 调度模块

协程、Timer、Event 和 FSM 已构成基础调度层，均由 `ManagerHub` 驱动。下一步应统一取消和生命周期归属，逐步引入：

- `TimerHandle`
- `CoroutineHandle`
- `EventBinding`
- `FsmHandle`
- `GameScope` 或 `LifecycleScope`

目标是让场景或业务模块能够集中清理自己创建的计时器、协程、事件、资源和 UI。

### 3.6 本地存档

`LocalDataManager.Save<T>()` 存在明确 Bug：缓存已存在时调用了 `_cacheDict.Add`，会抛出重复键异常。应改为直接赋值：

```csharp
_cacheDict[dataType] = data;
```

其他待完善项：

- 存档版本和迁移机制。
- 损坏恢复和备份文件。
- 异步保存，避免主线程卡顿。
- 角色 ID 的空值和路径安全校验。
- 角色切换时自动清理或切换缓存。
- 明确客户端硬编码密钥不能作为真正安全保护。

### 3.7 GameLogic

当前 `GameLogic` 主要包含本地化数据派生类，业务层较薄。尚未形成完整的启动、登录、玩家数据、主城或战斗流程，因此下一阶段应优先做一个可运行的最小业务闭环。

## 4. 开发优先级

### P0：立即修复

1. 修复 `LocalDataManager.Save<T>()` 缓存反逻辑。
2. 检查所有 Manager 的 `Login` 和 `Logout` 是否幂等。
3. 检查 UI 未初始化、异步加载失败和回调重入路径。
4. 检查 Asset 加载完成与释放同时发生时的状态一致性。
5. 检查场景 `OnEnter` 抛异常后的恢复状态。

### P1：补齐主链路

1. 新增 `GameBootstrap`。
2. 新增启动状态机。
3. 建立配置加载入口和 `ConfigManager`。
4. 初始化本地化、UI Root、资源和基础 Manager。
5. 建立登录场景和主场景。
6. 完成登录页到主界面的最小闭环。

### P2：业务基础设施

1. 网络传输和请求调度抽象。
2. 账号登录和连接状态管理。
3. 玩家数据和本地数据分层。
4. 资源句柄、取消和资源诊断。
5. UI 管理器逐步拆分。
6. 生命周期 Scope。

### P3：性能与发布能力

1. 资源引用泄漏检测。
2. UI 和场景切换耗时统计。
3. GC 和内存诊断。
4. 日志滚动、flush 和线上上下文。
5. 存档迁移、损坏恢复和版本校验。
6. 配置校验和 Addressables 构建检查。

## 5. 分阶段计划

### 阶段一：框架稳定化

目标：让现有基础模块达到可依赖状态。

任务：

1. 修复本地存档缓存 Bug。
2. 验证所有 Manager 的重复 Login/Logout。
3. 补充 Timer、Event、FSM、Scene、UI、Asset 和存档测试。
4. 增加关键错误和资源状态诊断。
5. 执行 Debug 构建并清理新增警告。

验收标准：

- `dotnet build "GameProject\\GameProject.sln" -c Debug` 成功。
- 0 个新增错误和警告。
- 核心 Manager 可重复 Login/Logout。
- 核心模块具备最小行为测试。

### 阶段二：启动与配置闭环

建议新增：

```text
GameLogic/Runtime/Bootstrap/GameBootstrap.cs
GameLogic/Runtime/Bootstrap/GameStartupFsm.cs
GameLogic/Runtime/Config/ConfigManager.cs
GameLogic/Runtime/Config/ConfigLoader.cs
```

启动流程：

```text
Unity 启动
    -> ManagerHub.Initialize
    -> ManagerHub.Login
    -> 加载配置
    -> 初始化本地化
    -> 初始化 UI Root
    -> 注册逻辑场景
    -> 进入 LoginScene
    -> 进入 MainScene
```

最小 Demo 应支持：启动页、登录页、主界面、Window、Tips、语言切换和本地数据保存。

### 阶段三：网络与账号

建议模块：

```text
GameNative/Runtime/Network/
GameEngine/Runtime/Network/
GameLogic/Runtime/Account/
```

最小能力：请求调度、超时、重试、心跳、连接状态、Token、断线重连和登录态生命周期。

### 阶段四：数据驱动业务

建议建立：

```text
GameSystem
GameModule
GameScope
ConfigManager
PlayerDataManager
```

业务模块应明确初始化入口、依赖关系、数据所有权、事件订阅、资源所有权、退出清理和跨场景存活规则。

### 阶段五：性能与发布

完善资源泄漏检测、UI/场景性能统计、内存和 GC 监控、日志上传、存档迁移、配置校验和开发者调试面板。

## 6. 推荐目标架构

```text
GameBootstrap
    |
    +-- Startup FSM
            |
            +-- Framework Initialization
            |       +-- ManagerHub
            |       +-- Log
            |       +-- Asset
            |       +-- Localization
            |
            +-- Config Initialization
            |       +-- ConfigManager
            |       +-- Generated Configs
            |
            +-- Network Initialization
            |       +-- NetworkManager
            |       +-- ConnectionState
            |
            +-- Account Login
            |       +-- AccountManager
            |       +-- PlayerDataManager
            |
            +-- Scene Entry
                    +-- LoginScene
                    +-- LobbyScene
                    +-- BattleScene
```

业务运行期间由 `GameScope` 管理事件、Timer、Coroutine、Asset、UI 和网络请求的所有权。

## 7. 下一步具体任务

建议按以下顺序实施：

1. 修复 `LocalDataManager.Save<T>()`。
2. 对 ManagerHub 生命周期和各 Manager 清理行为进行验证。
3. 实现 `GameBootstrap`。
4. 实现启动 FSM。
5. 接通 Excel 配置到运行时 `ConfigManager` 的链路。
6. 创建 LoginScene 和 MainScene 的最小业务样例。
7. 为登录页、主界面、本地化和存档增加 Unity 场景验证。
8. 在闭环稳定后实现网络与账号系统。

## 8. 验证要求

代码修改后至少执行：

```powershell
dotnet build "GameProject\\GameProject.sln" -c Debug
git status --short
```

涉及 Unity 行为时，使用 Unity 2022.3.62f2 打开 `GameClient/`，确认 Console 无 DLL 导入、类型加载或脚本错误，并运行对应场景。未启动 Unity 时，只能说明完成了 .NET 编译验证，不能视为 Unity 或 Play Mode 验证完成。
