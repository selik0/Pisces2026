# Pisces2026 Workspace Guide

## 项目概述

本仓库是基于 Unity 的游戏框架工程，使用 Unity 2022.3.62f2及以上版本。

工程采用源码与 Unity 客户端分离的结构：

- `GameProject/` 保存 C# 框架、业务和编辑器工具源码，以 `.NET Framework 4.7.2` 类库形式编译。
- `GameClient/` 是 Unity 客户端工程，使用 `GameProject` 编译到 `Assets` 下的 DLL。
- `Excel/` 保存策划配置表和 Excel 模板。
- `GameProject/lib/` 保存 Unity 和第三方编译依赖，不是本仓库源码的构建产物。

修改功能时应优先修改 `GameProject` 中的源码，不要反编译或直接修改 `GameClient/Assets` 下的 DLL。

## 目录职责

### GameProject

`GameProject/GameProject.sln` 包含以下项目：

- `GameEngine`：核心运行时框架。
- `GameEngineEditor`：Unity Editor 工具、Inspector、UI 代码生成和 Excel 工具。
- `GameLogic`：游戏业务逻辑程序集。
- `GameNative`：底层或原生能力程序集。
- `GameProto`：协议程序集。

源码目录：

- `GameProject/GameEngine/Runtime/`
- `GameProject/GameEngineEditor/Editor/`
- `GameProject/GameLogic/Runtime/`
- `GameProject/GameNative/Runtime/`
- `GameProject/GameProto/Runtime/`

项目依赖关系：

```text
GameNative ----+
GameProto  ----+--> GameLogic
GameEngine ----+

GameEngine --------> GameEngineEditor
```

所有项目均以 `net472` 为目标框架。`GameEngine` 和 `GameLogic` 引用 `GameProject/lib/*.dll`；`GameEngineEditor` 还引用 `GameProject/lib/Editor/*.dll`。

### GameClient

`GameClient/` 是 DLL 的消费端，而不是主要框架源码目录。

重要目录：

- `GameClient/Assets/LogicDll/`：运行时 DLL。
- `GameClient/Assets/Editor/Dll/`：仅供 Unity Editor 使用的 DLL。
- `GameClient/Assets/Editor/Config/`：编辑器配置资产。
- `GameClient/Assets/Scenes/`：Unity 场景。
- `GameClient/Assets/Settings/`：URP 2D 等项目设置。
- `GameClient/Packages/`：Unity Package Manager 配置。
- `GameClient/ProjectSettings/`：Unity 项目设置。

不要编辑或提交 Unity 本地生成目录，例如：

- `GameClient/Library/`
- `GameClient/Temp/`
- `GameClient/Obj/`
- `GameClient/Logs/`
- `GameClient/UserSettings/`
- `GameClient/.vs/`

### GameProject/lib

`GameProject/lib/` 中的 DLL 是 Unity 和第三方编译依赖，包括 UnityEngine、UnityEditor、TextMeshPro 和 EPPlus 等。

- 不要将这些 DLL 当成本项目的构建产物删除。
- 不要直接修改供应商 DLL。
- 更新 Unity 依赖 DLL 时，必须与 Unity 版本保持一致。

## 运行时子系统

`GameProject/GameEngine/Runtime/` 当前包含以下主要模块：

- `Coroutine/`：自定义协程调度器。
- `Event/`：类型安全事件总线和生命周期绑定。
- `Fsm/`：有限状态机管理。
- `Log/`：Unity Console 与异步文件日志。
- `RedDot/`：基于路径树的红点系统。
- `Scene/`：框架层逻辑场景管理。
- `Timer/`：延迟和重复定时任务。
- `UI/`：UIEntity、UIBrick、UIView、UIWidget 和 UI 生命周期。

Coroutine、FSM、Scene 和 Timer 等系统需要由游戏主循环调用对应的 `Tick`，不能假设它们像 Unity `MonoBehaviour.Update` 一样自动运行。

## 构建

从仓库根目录执行：

```powershell
dotnet build "GameProject\GameProject.sln" -c Debug
```

Release 构建：

```powershell
dotnet build "GameProject\GameProject.sln" -c Release
```

如果当前工作目录已经是 `GameProject/`，使用：

```powershell
dotnet build "GameProject.sln" -c Debug
```

构建输出：

- `GameClient/Assets/LogicDll/GameEngine.dll`
- `GameClient/Assets/LogicDll/GameLogic.dll`
- `GameClient/Assets/LogicDll/GameNative.dll`
- `GameClient/Assets/LogicDll/GameProto.dll`
- `GameClient/Assets/Editor/Dll/GameEngineEditor.dll`

这些 DLL 当前是 Unity 工程的一部分，并可能被 Git 跟踪。运行构建会覆盖它们，因此构建前后都要检查工作区状态。

不要手工编辑以下生成物：

- `GameProject/**/bin/`
- `GameProject/**/obj/`
- `GameClient/Assets/**/*.pdb`
- MSBuild 生成的 `AssemblyInfo.cs`、缓存和文件列表。

## 验证

当前仓库已安装 Unity Test Framework，但没有发现正式的自动化测试程序集或测试用例。不要把“编译成功”描述成“测试通过”。

最低验证流程：

1. 运行 `dotnet build "GameProject\GameProject.sln" -c Debug`。
2. 确认构建结果为 0 个错误和 0 个警告，或说明已有警告。
3. 检查 `git status --short`，确认只改动了预期源码和构建 DLL。
4. 涉及 Unity 行为时，使用 Unity 2022.3.62f2 打开 `GameClient/`。
5. 确认 Unity Console 没有 DLL 导入、类型加载或脚本错误。
6. 根据改动运行 `SampleScene.unity` 或对应功能场景进行验证。

若没有启动 Unity，应明确说明只完成了 .NET 编译验证，未完成 Unity Editor 或 Play Mode 验证。

## C# 代码格式

以下格式规则适用于整个工作空间中的 C# 代码。

### 缩进与大括号

- 使用 4 个空格缩进，不使用 Tab。
- 使用 Allman 大括号风格，左大括号独占一行。
- `if`、`else`、`for`、`foreach`、`while`、`do`、`using`、`lock` 等控制流语句必须始终使用 `{}`。
- 即使语句体只有一行，也不能省略 `{}`。
- 控制流条件和执行语句不能写在同一行。

正确：

```csharp
if (condition)
{
    Execute();
}

foreach (Item item in items)
{
    Process(item);
}
```

错误：

```csharp
if (condition) Execute();

if (condition)
    Execute();

foreach (Item item in items)
    Process(item);
```

### 命名

- 类型、枚举、方法、属性和公开成员使用 `PascalCase`。
- 接口使用 `I` 前缀，例如 `IScene`、`IFsm`、`ILogHandler`。
- 私有实例字段使用 `_camelCase`。
- 局部变量和参数使用 `camelCase`。
- 静态系统入口通常命名为 `XxxSystem`。
- 管理器和调度器使用 `XxxManager`、`XxxScheduler`。
- 句柄类型使用 `XxxHandle`。
- 避免无意义缩写，名称应表达生命周期和所有权语义。

### 文件与结构

- 一个文件通常只定义一个主要类型，文件名与主要类型名一致。
- `using` 放在文件顶部，删除未使用的 `using`。
- 保持现有命名空间，不要仅根据目录名批量修改命名空间。
- 核心运行时通常使用 `GameEngine` 命名空间。
- 编辑器工具通常使用 `GameEngineEditor` 命名空间，但存在历史例外。
- 不要只为缩短方法而创建没有复用价值的辅助类型或包装层。

### 注释与文档

- 公共 API、生命周期约束和不直观行为使用简洁的 XML 文档注释。
- 注释应解释原因、约束或所有权，不要复述代码。
- 仓库现有注释以中文为主，新注释应与所在文件保持一致。
- 修改复杂状态机时，优先通过清晰命名和状态结构表达行为，只为关键重入或清理规则添加注释。

### 异常与清理

- 框架级事件、调度器和生命周期清理不能因一个监听器异常而跳过其他清理。
- 注册和注销、创建和销毁、绑定和解绑必须成对处理。
- 使用 `try/finally` 或等价结构保证状态复位和资源释放。
- 不要静默吞掉异常；若框架必须继续运行，应记录异常并保持状态一致。
- Unity 对象需要考虑 Unity 的假 null 语义。

## UI 开发约定

UI 框架源码位于：

```text
GameProject/GameEngine/Runtime/UI/
```

UI 编辑器代码位于：

```text
GameProject/GameEngineEditor/Editor/UI/
```

UI 约定：

- `UIEntity` 保存编辑器收集的组件引用。
- `UIBrick` 管理 UI 逻辑生命周期。
- `UIView` 表示界面。
- `UIWidget` 表示可组合组件。
- UI 组件属性通常使用 `btn`、`img`、`rect` 等前缀和驼峰名称。
- UI 根节点名称应是合法 C# 类型标识符，不要使用 `-`、`.`、`/` 或数字开头的名称。

UI 代码生成器会覆盖同名生成文件。执行生成前必须：

1. 检查目标文件是否已存在。
2. 检查目标文件是否包含用户修改。
3. 不要在自动生成文件中放置手写业务逻辑。
4. 将手写逻辑放在独立的同名 `partial` 类文件中。
5. 未经明确要求，不要自动触发 UI 代码生成。

生成文件通常会包含“自动生成，请勿手动修改”提示，应遵守该提示。

## Excel 开发约定

- Excel 模板和配置输入位于 `Excel/`。
- Excel 编辑器工具使用 EPPlus，源码位于 `GameProject/GameEngineEditor/Editor/Excel/`。
- 不要无确认覆盖已有 `.xlsx` 文件。
- 修改 Excel、CSV 或转表流程时，应使用工作空间提供的 Excel/配置转换工具并验证输出。
- `GameProject/lib/Editor/EPPlus.dll` 和 `GameClient/Assets/Editor/Dll/EPPlus.dll` 是第三方依赖，不是项目生成物。

## Unity 资产与 Meta 文件

- 移动、重命名或删除 Unity 资产时，必须同步处理对应 `.meta` 文件。
- 不要删除 DLL 的 `.meta` 后让 Unity 自动重建，否则 GUID 和平台导入配置会变化。
- 不要手工批量修改 `.unity`、`.prefab`、`.asset` 或 `ProjectSettings` YAML。
- 涉及序列化引用或 ScriptableObject 类型迁移时，应在 Unity Editor 中验证。
- `GameEngineEditor.dll` 和 EPPlus 必须保留在 Editor 专属目录和导入边界内。

## Git 与协作安全

修改前执行：

```powershell
git status --short
```

修改后再次执行：

```powershell
git status --short
git diff --check
```

协作规则：

- 工作区可能包含用户或其他代理的未提交修改。
- 不要还原、覆盖、格式化或删除不属于当前任务的修改。
- 目标文件已有修改时，先阅读并在现有改动基础上继续工作。
- 不要使用 `git reset --hard`、`git checkout -- <file>`、`git clean` 等破坏性命令。
- 不要自动提交或推送；仅在用户明确要求时执行。
- 不要因为 DLL、场景或资产是生成结果就擅自删除它们。
- 构建后不要恢复 DLL；应报告构建更新了哪些受跟踪产物。
- 不要提交 Unity `Library`、日志、用户设置、MSBuild `obj` 或 PDB。

## 修改原则

- 先阅读相关实现和调用链，再修改代码。
- 优先选择最小且正确的改动。
- 保持现有架构和命名风格，不为假设中的未来需求增加兼容层。
- 修改公共 API 前搜索全部调用方和派生类。
- 修改生命周期、事件、资源释放或异步代码时，检查正常路径、重复调用、异常路径和回调重入。
- 不要把构建生成的 DLL 当成源码修改目标。
- 完成后执行与改动范围相匹配的编译和 Unity 验证，并准确报告未执行的验证。
