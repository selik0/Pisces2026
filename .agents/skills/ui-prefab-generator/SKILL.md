---
name: ui-prefab-generator
slug: ui-prefab-generator
displayName: Unity UI Prefab 与 UIEntity 生成器
description: 用于 Pisces2026 Unity 项目中创建或修改 UI 界面。凡是用户要求生成 UI、登录界面、进入游戏界面、窗口、面板、弹窗，或明确要求生成 Prefab、自动配置 UIEntity、收集组件、调用 UIEntity 代码生成器时，都应使用此 skill。它负责检查现有 UI 约定，创建合法的 Unity Prefab，配置 UIEntity 组件收集引用，并在用户明确要求时通过 Unity Editor 生成绑定代码；同时创建独立的 partial 逻辑代码、接入 UIManager，并完成 .NET 编译和资源检查。
version: 1.0.0
license: MIT
---

# Unity UI Prefab 生成规范

你正在修改 Pisces2026 Unity 工程。目标是让一个 UI 从 Prefab、UIEntity 绑定、生成绑定代码到运行时逻辑完整可用，而不是只创建一个视觉占位文件。

## 适用范围

当用户提出以下任一类请求时使用本 skill：

- 创建登录、进入游戏、主界面、公告、设置、背包等 UI。
- 在 `GameClient/Assets/GameAssets/UIPrefab/` 下生成或修改 Prefab。
- 自动绑定 `UIEntity`、收集 `Button`、`Image`、`TextMeshProUGUI`、`TMP_InputField` 等组件。
- 调用 UIEntity Inspector 的代码生成功能。
- 创建与 Prefab 对应的 `UIView`、`UIWidget` 或 partial 逻辑类。

## 开始前检查

1. 先执行 `git status --short`，不要覆盖用户或其他代理已有改动。
2. 阅读仓库根目录 `AGENTS.md`，遵守 Unity 资产、UI 生命周期、日志和回调规范。
3. 检查 `GameClient/Assets/GameAssets/UIPrefab/` 下的同类 Prefab，优先复用项目现有的 Canvas、字体、材质、按钮和输入框序列化方式。
4. 阅读 `GameClient/Assets/Editor/Config/ComponentBindConfig.asset`，确认节点前缀和组件类型的绑定规则。
5. 搜索同名 UI 类型、Prefab、生成绑定文件和逻辑文件，避免覆盖已有手写逻辑。

## Prefab 规则

### 路径与命名

- Prefab 默认放在 `GameClient/Assets/GameAssets/UIPrefab/<子目录>/`。
- 根节点名称必须是合法 C# 类型标识符，例如 `LoginUI`、`EnterGameUI`。
- 根节点必须包含 `RectTransform`、UI 需要的 Canvas/GraphicRaycaster，以及 `UIEntity`。
- 移动、重命名或新增 Prefab 时同步维护 `.meta` 文件。

### 节点前缀

根据 `ComponentBindConfig.asset` 使用前缀命名：

- `btn_`：`UnityEngine.UI.Button`
- `img_`：`UnityEngine.UI.Image`
- `txt_`：优先 `TMPro.TextMeshProUGUI`
- `input_`：优先 `TMPro.TMP_InputField`
- `tog_`：`UnityEngine.UI.Toggle`
- `sld_`：`UnityEngine.UI.Slider`
- `drop_`：`TMPro.TMP_Dropdown`

同一节点需要被多个组件绑定时，确保命名和组件配置不会产生重复属性名。节点名转成属性名后必须是合法 C# 标识符。

### 绑定流程

用户明确要求“自动绑定 UIEntity 并调用 UIEntity 生成代码”时，必须完成以下流程：

1. 在 Prefab 根节点挂载 `UIEntity`。
2. 使用项目 UIEntity Inspector 的“收集组件/生成代码”功能，而不是只手写 `ComponentList`。
3. 生成前检查目标绑定文件是否存在。
4. 如果绑定文件带有“自动生成，请勿手动修改”标记，可以由生成器覆盖；否则先停止并保留用户手写文件。
5. 生成代码后确认输出路径与 Prefab 子目录一致：
   - `GameProject/GameLogic/Runtime/UI/Generated/<子目录>/<ClassName>_Bind.cs`
   - `GameProject/GameLogic/Runtime/UI/Logic/<子目录>/<ClassName>.cs`
6. 不要把业务逻辑写进 `_Bind.cs`。业务逻辑必须放在独立的同名 partial 类中。
7. 如果当前环境没有 Unity Editor，不能声称已经调用了 Inspector 生成器；应说明限制，并只在用户明确接受时提供等价的临时绑定文件。

## 运行时逻辑

- 界面继承 `UIView` 或 `UIView<TData>`，设置正确的 `PrefabPath`。
- 通过 `UIManager.Instance.OpenView<TView>()` 打开界面，通过 `UIManager.Instance.CloseView(this)` 关闭自身。
- 输入框、按钮和事件必须在绑定文件的生命周期中成对注册和注销。
- 异步、定时器和流程对象必须在 `OnClose` 或 `OnDestroy` 中停止、解绑并释放。
- 回调直接调用，例如 `callback?.Invoke()`；不要使用 `GetInvocationList()` 逐个执行，也不要用仅为捕获并记录后继续执行的 `try/catch` 包装回调。
- 日志统一使用 `GameEngine.Log` 或 `Log.GetLogger`，不要使用 `Debug.Log` 或 `Console.WriteLine`。
- 编辑器专用行为使用 `#if UNITY_EDITOR`，正式包行为保留明确的后续接入点，不要伪造 SDK 调用。

## 验证

1. 执行 `dotnet build "GameProject\\GameProject.sln" -c Debug`。
2. 检查编译结果为 0 个错误和 0 个警告，或准确报告已有警告。
3. 执行 `git diff --check`。
4. 检查 `git status --short`，确认只包含预期源码、Prefab、meta 和构建生成 DLL。
5. 如果没有启动 Unity Editor，明确说明未完成 Prefab 导入、组件引用、布局和 Play Mode 验证。
6. Unity 验证时确认：Prefab 无 Missing Script、UIEntity 引用完整、生成类能加载、按钮事件能触发、关闭界面后没有残留回调或定时器。

## 输出报告

完成后简要报告：

- 新增或修改的 Prefab 和 `.meta` 路径。
- UIEntity 收集到的关键组件和生成绑定文件路径。
- 运行时逻辑文件及 UIManager 接入点。
- 自动化模拟或业务流程的行为。
- 编译结果。
- 是否完成 Unity Editor/Play Mode 验证，以及未完成的风险。
