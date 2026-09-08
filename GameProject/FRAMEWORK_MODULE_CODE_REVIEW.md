# GameProject 现有功能模块代码审查

## 1. 审查范围与结论

本次对 `GameProject/GameEngine`、`GameProject/GameNative` 和 `GameProject/GameLogic` 进行了静态源码审查，覆盖 Event、Timer、Coroutine、FSM、Scene、UI、Asset、Audio、Localization、RedDot、Log 和 Storage 等模块。

本报告中的“已确认 Bug”均来自源码可直接证明的行为；“风险项”表示需要结合运行时场景或补充代码验证。此次未运行 .NET 构建、Unity Editor 或 Play Mode，因此不能将本报告视为运行测试结果。

总体判断：当前框架已经具备较完整的基础模块，但事件分发、文件系统安全、存档可靠性和资源释放仍存在需要优先处理的问题。建议先完成 P0/P1 修复，再进行模块拆分和功能扩展。

## 2. 高优先级问题

### H-01 Event：回调修改订阅集合会破坏遍历

文件：`GameEngine/Runtime/Event/EventManager.cs`，约第 127～135 行。

`Emit` 直接调用 `EventBinding.Invoke`，没有对监听器集合创建快照，也没有延迟变更队列。监听器在回调中对同一个事件执行 `Subscribe` 或 `Unsubscribe` 时，可能修改正在遍历的集合，导致：

- `InvalidOperationException`；
- 后续监听器无法收到事件；
- 事件分发处于部分完成状态；
- 清理流程不完整。

建议：

1. 分发前复制监听器快照；或
2. 在分发期间记录新增和删除操作，分发完成后统一提交；
3. 明确同一事件的重入语义。

### H-02 Event：单个监听器异常会中断整个广播

文件：`GameEngine/Runtime/Event/EventManager.cs`，约第 134、144、154、164 行。

事件绑定调用外层没有逐监听器异常隔离。任一监听器抛出异常后，后续监听器不会执行，可能导致 UI、Timer、Coroutine 或其他模块的注销流程被跳过。

建议在每个监听器调用处使用 `try/catch`，记录异常后继续执行剩余监听器。若事件分发具有事务语义，应明确采用“继续广播”还是“立即终止”，不能仅依赖调用方约定。

### H-03 Native FileSystem：路径解析存在目录穿越风险

文件：`GameNative/Runtime/File/FileSystem.cs`，约第 44～53 行。

`Path.Combine(PersistentRoot, relativePath)` 和 `Path.Combine(StreamingRoot, relativePath)` 没有验证规范化后的路径仍位于根目录内。传入 `..\\OtherFile`、绝对路径或盘符路径时，可能读取、覆盖或删除根目录之外的文件。

建议：

1. 拒绝 rooted path；
2. 使用 `Path.GetFullPath` 规范化路径；
3. 比较规范化路径和规范化根目录的前缀，并验证目录边界；
4. 对存档名、配置名和外部输入统一做路径安全校验。

### H-04 Storage：`LocalDataManager.Save` 缓存逻辑错误

文件：`GameEngine/Runtime/Storage/LocalDataManager.cs`，约第 37～45 行。

当前代码在缓存已存在时调用 `_cacheDict.Add(dataType, data)`，会因重复键抛出异常；缓存不存在时才使用索引器赋值。正确逻辑应直接使用：

```csharp
_cacheDict[dataType] = data;
```

该问题会影响同一类型存档的第二次保存，应列为立即修复项。

## 3. 中优先级问题

### M-01 Timer/Coroutine：Tick 期间操作集合的语义不稳定

文件：

- `GameEngine/Runtime/Timer/TimerManager.cs`，约第 143～147 行；
- `GameEngine/Runtime/Coroutine/CoroutineManager.cs`，约第 98～101 行。

两个模块均直接遍历可变列表。回调如果触发 `StopAll`、清理或其他生命周期操作，当前帧执行集合可能发生变化，导致对象被跳过、重复处理或新增对象提前参与当前 Tick。

建议：

- Tick 开始时固定本帧执行范围；
- 新增、删除和 StopAll 统一延迟到 Tick 结束提交；
- 增加 `_isTicking` 状态防止重入；
- 明确停止当前对象后是否允许继续执行当前回调之后的逻辑。

### M-02 Coroutine：`Start(null)` 未立即拒绝

文件：`GameEngine/Runtime/Coroutine/CoroutineManager.cs`，约第 26～32 行。

`Start` 未校验 `IEnumerator` 是否为 null，错误会延迟到下一次 Tick 才暴露，可能产生无效协程条目或空引用异常。

建议在入口处立即校验，使用项目统一约定的方式返回无效 ID 或抛出明确的 `ArgumentNullException`。

### M-03 Timer：时间推进契约与实现不一致

文件：`GameEngine/Runtime/Timer/TimerManager.cs`，约第 119～125 行。

文档描述存在 `deltaTime` 参数，但实际 `Tick()` 没有参数，并直接调用 `TimerEntry.Tick()`。这会导致时间源难以注入，测试、服务器时间、固定时间步和暂停场景的行为不易控制。

建议统一 API，例如由 Manager 接收明确的 `deltaTime`，并将是否使用 `Time.timeScale` 的策略显式传入或绑定到 Timer 配置。

### M-04 Asset：Addressables operation 可重复 Release

文件：`GameEngine/Runtime/Asset/AddressablesAssetProvider.cs`，约第 16～31 行。

`Operation<T>.Release()` 没有记录释放状态，也没有校验 Handle 是否仍有效。重复释放可能导致 Addressables 引用计数多减、资源提前卸载或 Unity 内部异常。

建议：

- 增加 `_released` 状态；
- 重复释放时忽略并记录警告；
- 释放前检查 `Handle.IsValid()`；
- 释放后让资源属性进入安全的无效状态。

### M-05 Asset：失败和提前释放的所有权语义不明确

文件：`GameEngine/Runtime/Asset/AddressablesAssetProvider.cs`。

当前没有明确说明以下情况的行为：加载失败是否必须释放、未完成操作是否允许释放、重复释放是否安全、空地址如何处理。调用方容易在失败路径泄漏资源，或在成功回调之后重复释放。

建议定义统一资源状态和所有权规则，并优先提供 `AssetHandle<T>`，让句柄持有资源身份和释放状态，减少通过地址和类型手工释放的错误。

### M-06 Native FileSystem：异步回调异常未隔离

文件：`GameNative/Runtime/File/FileSystem.cs`，约第 256～280 行。

Android/WebGL 异步分支直接调用 `onSuccess` 和 `onError`，没有保护用户回调异常；同步分支则可能把用户成功回调异常误判为文件读取异常并再次调用 `onError`。

建议将“文件操作异常”和“用户回调异常”分离处理：文件读取使用一层 `try/catch`，回调执行使用独立的安全调用函数，确保成功回调异常不会被转换成 IO 错误。

## 4. 存储与文件系统可靠性

### L-01 `WriteAllText` 的 append 参数无效

文件：`GameNative/Runtime/File/FileSystem.cs`，约第 116～120 行。

公开 API 接收 `append` 参数，但始终调用 `File.WriteAllText`，因此 `append: true` 仍然会覆盖文件。该问题可能造成日志、缓存或增量数据丢失。

建议根据 `append` 选择 `File.AppendAllText` 或使用带 append 参数的 `StreamWriter`。

### L-02 JSON 损坏与合法默认值无法区分

文件：`GameNative/Runtime/File/FileSystem.cs`，约第 145～160 行。

JSON 错误返回 `default(T)`，调用方无法区分文件不存在、文件为空、JSON 损坏和合法默认值。引用类型可能返回 null，值类型可能被当作合法数据继续运行。

建议返回包含状态的结果类型，至少区分：`NotFound`、`Success`、`InvalidFormat`、`IOError`，并由存档层决定是否恢复备份或创建新数据。

### L-03 存档写入非原子

文件：`GameNative/Runtime/File/FileSystem.cs`，约第 131～135、170～173 行。

JSON 和二进制数据直接写入目标文件。进程崩溃、强制退出、磁盘空间不足或断电时可能留下半写文件。

建议写入临时文件并 Flush，完成后使用原子替换或移动；重要存档保留一个备份文件，并在读取时进行校验和恢复。

### L-04 文件 API 参数和路径校验不统一

文件：`GameNative/Runtime/File/FileSystem.cs`，约第 44～53、88～93、123～140 行。

空路径、绝对路径、非法字符和越界路径可能产生不一致异常。建议统一入口校验，并定义明确的异常或错误结果协议。

## 5. 其他模块审查建议

### FSM

暂未确认高置信度 Bug。应重点测试：

- `Enter` 或 `Exit` 中再次切换状态；
- 重复切换到当前状态；
- 删除 FSM 时当前状态是否完整退出；
- Tick 中状态被销毁或切换后的继续执行行为；
- 重复注册和不存在状态的处理。

### Scene

应重点验证：

- 场景切换请求重入；
- `OnEnter` 异常后的 `CurrentScene` 状态；
- 异步加载回调在旧场景销毁后仍执行；
- Unity 场景加载与逻辑场景生命周期是否一致；
- 登出时是否取消未完成的切换请求。

### UI

UIManager 功能完整但职责较重。应重点测试：

- Open/Close 重复调用；
- `OnOpen` 或 `ViewClosed` 回调中再次打开、关闭 UI；
- 父界面关闭时子界面和 Widget 的级联清理；
- 关闭非栈顶界面后的层级可见性；
- 异步加载完成与 UI 销毁之间的竞态。

建议逐步拆分 `UIViewFactory`、`UILayerController`、`UINavigationStack`、`UIWindowOwnership` 和 `UIOpenQueue`，并增加异步打开、失败和取消模型。

### Audio

暂未确认直接 Bug。应核查 AudioClip、AudioSource、播放句柄的所有权，以及停止、切场景、登出时的释放行为。还应验证同一音频重复播放、暂停恢复、时间缩放和 Addressables 资源生命周期。

### Localization

应核查不支持语言、缺失 key 和缺失 Sprite/Audio/Text 数据的 fallback；语言切换事件是否可重入；数据替换期间 UI 是否可能访问半初始化状态；运行时语言映射是否可能被意外修改。

### RedDot

应核查节点删除时父子节点和监听器是否完整解绑，重复创建同一路径的行为，父节点聚合更新的递归重入，以及清理后旧节点句柄是否仍可修改红点树。

### Log

应核查 FileLogHandler 多线程写入保护、退出时 Flush、关闭与后台写入并发、Handler 异常隔离，以及重复初始化时是否重复注册 Unity Console Handler。

## 6. 建议实施顺序

### 第一批：安全与数据完整性

1. 修复 `LocalDataManager.Save<T>()`。
2. 修复 FileSystem 根目录校验和路径穿越问题。
3. 修复 `WriteAllText` 的 append 行为。
4. 增加存档临时文件、原子替换和备份恢复。
5. 区分 JSON 损坏、文件不存在和合法默认值。

### 第二批：事件与调度稳定性

1. Event 使用快照或延迟变更队列。
2. Event 单监听器异常隔离。
3. Timer/Coroutine Tick 使用稳定执行范围。
4. 增加 Tick 重入保护。
5. 校验 Coroutine null 参数。
6. 统一 Timer 的 deltaTime 契约。

### 第三批：资源和 UI 生命周期

1. Addressables operation 增加幂等 Release。
2. 引入 `AssetHandle<T>`。
3. 明确加载失败、取消和提前释放语义。
4. 对 UI 异步打开、关闭、销毁和回调重入增加测试。
5. 逐步拆分 UIManager 的职责。

### 第四批：专项测试和诊断

1. 补充 FSM、Scene、UI、Audio、Localization、RedDot 和 Log 生命周期测试。
2. 增加重复初始化、重复销毁、Login/Logout 重入测试。
3. 增加资源引用泄漏、UI 打开耗时和场景切换耗时诊断。
4. 对跨线程 API 增加主线程约束或同步保护。

## 7. 验收标准

代码修复后至少执行：

```powershell
dotnet build "GameProject\\GameProject.sln" -c Debug
git status --short
git diff --check
```

涉及 Unity 行为时，应使用 Unity 2022.3.62f2 打开 `GameClient/`，确认无 DLL 导入、类型加载或脚本错误，并运行对应场景验证 UI、资源、场景和存档流程。
