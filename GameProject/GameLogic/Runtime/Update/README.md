# 启动更新流程

`GameUpdateFlow` 在 `GameLogic` 中提供启动更新入口，由现有主循环的 `FsmManager.Instance.Tick` 驱动。启动器需持有流程实例，并在加载业务 UI、场景和资源之前调用 `Start()`。

## 接入示例

```csharp
private GameUpdateFlow _updateFlow;

private void StartUpdate()
{
    _updateFlow = new GameUpdateFlow(new GameUpdateSettings
    {
        VersionCheckUrl = "https://your-update-server/version?platform=android&channel=official",
        DownloadLabels = new[] { "Preload" }
    });
    _updateFlow.StageChanged += RefreshUpdateUI;
    _updateFlow.ProgressChanged += RefreshUpdateUI;
    _updateFlow.Completed += EnterLogin;
    _updateFlow.Failed += ShowRetryButton;
    _updateFlow.Start();
}
```

- `RefreshUpdateUI` 根据 `Stage` 显示状态。强更展示 `VersionInfo.Notice`，按钮调用 `OpenForceUpdateUrl()`。
- 热更确认页显示 `DownloadBytes`，按钮调用 `ConfirmHotUpdate()`。
- 下载页使用 `DownloadedBytes` 和 `DownloadProgress` 显示进度。
- 失败页显示 `ErrorMessage`，重试按钮调用 `Retry()`。
- `Completed` 回调进入登录流程；不要在这里销毁或创建 FSM，因为 `FsmManager.Tick` 正在遍历集合。将 FSM 创建/销毁安排在本次 Tick 结束之后。
- 启动器退出时解除事件订阅并调用 `Destroy()`。释放 Addressables 句柄不会立即取消底层下载，但流程不再使用该操作结果。

## 版本接口

接口返回以下 JSON，字段区分大小写：

```json
{
  "MinimumClientVersion": "1.2.0",
  "LatestClientVersion": "1.3.0",
  "InstallUrl": "https://your-download-server/game",
  "Notice": "请安装最新版本后重新启动游戏。"
}
```

版本使用一至四段非负整数，缺失段补零，例如 `1.2` 等同 `1.2.0`。默认读取 `Application.version`，也可通过 `ClientVersion` 指定渠道版本。

客户端低于 `MinimumClientVersion` 时停在强更阶段，打开 HTTP/HTTPS 安装地址后仍不进入游戏，必须安装并重启。`LatestClientVersion` 是可选的展示字段，不触发强更。接口失败、数据不合法和资源下载失败均进入失败阶段，不跳过检查。

## Addressables 热更

执行顺序：初始化 Addressables → 检查远端目录 → 更新目录 → 计算指定标签的缺失下载量 → 用户确认 → 下载依赖 → 完成。

发布时需配置远端 Catalog 和远端资源路径，关闭 Addressables 的启动自动目录更新（由此流程显式检查），上传内容更新生成的 Catalog、Hash 及 Bundle，并保证目标客户端可访问。更新检查必须在业务资源加载前完成。

`DownloadLabels` 应填写项目真实标签，默认空数组只更新目录，不预下载 Bundle；后续资源加载按需下载。指定标签后，即使目录没有变化，也会检查缓存中的缺失资源；重试可以复用已缓存 Bundle。

这里的热更是 **Addressables 资源热更**。当前项目未接入 HybridCLR 等代码热更运行环境；业务 DLL 的代码变更通过安装新客户端生效。
