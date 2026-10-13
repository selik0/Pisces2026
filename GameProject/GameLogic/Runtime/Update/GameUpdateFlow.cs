using System;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace GameLogic
{
    /// <summary>
    /// 启动更新流程。所有 API 在 Unity 主线程调用，由主循环的 FsmManager.Tick 推进。
    /// 强更等待安装并重启，只有热更成功才触发 Completed。应在加载业务资源前运行。
    /// </summary>
    public sealed class GameUpdateFlow
    {
        private readonly GameUpdateSettings _settings;
        private readonly string _fsmName = "GameUpdateFlow_" + Guid.NewGuid().ToString("N");
        private IFsm<GameUpdateFlow> _fsm;
        private UnityWebRequest _request;
        private AsyncOperationHandle _operation;
        private bool _hasOperation;
        private bool _downloadConfirmed;
        private List<string> _catalogs;
        private string[] _labels;
        private bool _simulateUpdate;
        private float _simulationProgress;

        public GameUpdateFlow(GameUpdateSettings settings = null)
        {
            _settings = settings ?? new GameUpdateSettings();
        }

        public GameUpdateStage Stage { get; private set; }
        public GameUpdateVersionInfo VersionInfo { get; private set; }
        public bool ForceUpdateRequired { get; private set; }
        public bool HotUpdateRequired { get; private set; }
        public string ErrorMessage { get; private set; }
        public long DownloadBytes { get; private set; }
        public long DownloadedBytes { get; private set; }
        public float DownloadProgress { get; private set; }

        /// <summary>阶段变化，UI 可按 Stage 显示强更、下载确认和重试按钮。</summary>
        public event Action StageChanged;
        public event Action ProgressChanged;
        public event Action Completed;
        public event Action Failed;

        /// <summary>配置版本接口后启动；重复调用不重复创建流程。</summary>
        public void Start()
        {
            if (_fsm != null && !_fsm.IsDestroyed)
            {
                return;
            }

            _fsm = FsmManager.Instance.CreateFsm(_fsmName, this, new UpdateState());
            if (_fsm != null)
            {
                _fsm.Start<UpdateState>();
            }
        }

        /// <summary>失败后重新检查版本及目录，已缓存的资源由 Addressables 复用。</summary>
        public void Retry()
        {
            if (Stage == GameUpdateStage.Failed && _fsm != null && !_fsm.IsDestroyed)
            {
                Begin();
            }
        }

        /// <summary>用户确认下载后调用，实际下载在下一次 Tick 开始。</summary>
        public void ConfirmHotUpdate()
        {
            if (Stage == GameUpdateStage.WaitingForHotUpdate)
            {
                _downloadConfirmed = true;
            }
        }

        /// <summary>打开商店或安装包地址。返回应用后仍然阻止进入游戏，需安装并重启。</summary>
        public void OpenForceUpdateUrl()
        {
            if (Stage != GameUpdateStage.WaitingForForceUpdate)
            {
                return;
            }

            try
            {
                Application.OpenURL(VersionInfo.InstallUrl);
            }
            catch (Exception exception)
            {
                Log.Error("[GameUpdateFlow] 打开安装地址失败", exception);
            }
        }

        /// <summary>释放请求和操作句柄。由启动器生命周期调用，不在 FSM Tick 回调内销毁管理器中的 FSM。</summary>
        public void Destroy()
        {
            var fsm = _fsm;
            _fsm = null;
            if (fsm != null && !fsm.IsDestroyed)
            {
                FsmManager.Instance.DestroyFsm(fsm);
            }

            Cleanup();
            Stage = GameUpdateStage.Idle;
        }

        private void Begin()
        {
            try
            {
                Cleanup();
                ErrorMessage = null;
                VersionInfo = null;
                ForceUpdateRequired = false;
                HotUpdateRequired = false;
                DownloadBytes = 0;
                DownloadedBytes = 0;
                DownloadProgress = 0;
                _downloadConfirmed = false;
                _catalogs = null;
                _labels = (string[])(_settings.DownloadLabels ?? Array.Empty<string>()).Clone();
                _simulateUpdate = _settings.SimulateUpdate;
                _simulationProgress = 0f;
                if (_simulateUpdate)
                {
                    SetStage(GameUpdateStage.CheckingVersion);
                    return;
                }

                RequireWebUrl(_settings.VersionCheckUrl);
                if (_settings.RequestTimeoutSeconds <= 0)
                {
                    throw new ArgumentException("版本请求超时时间必须大于零");
                }

                _request = UnityWebRequest.Get(_settings.VersionCheckUrl);
                _request.timeout = _settings.RequestTimeoutSeconds;
                _request.SendWebRequest();
                SetStage(GameUpdateStage.CheckingVersion);
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private void Tick()
        {
            try
            {
                if (_simulateUpdate)
                {
                    TickSimulation();
                    return;
                }

                if (Stage == GameUpdateStage.CheckingVersion)
                {
                    CheckVersion();
                    return;
                }

                if (Stage == GameUpdateStage.WaitingForHotUpdate && _downloadConfirmed)
                {
                    Track(Addressables.DownloadDependenciesAsync((System.Collections.IEnumerable)_labels,
                        Addressables.MergeMode.Union, false));
                    SetStage(GameUpdateStage.Downloading);
                    return;
                }

                if (!_hasOperation)
                {
                    return;
                }

                if (Stage == GameUpdateStage.Downloading)
                {
                    var status = _operation.GetDownloadStatus();
                    DownloadedBytes = status.DownloadedBytes;
                    DownloadProgress = status.Percent;
                    Notify(ProgressChanged);
                    if (!_hasOperation)
                    {
                        return;
                    }
                }

                if (!_operation.IsDone)
                {
                    return;
                }

                if (_operation.Status != AsyncOperationStatus.Succeeded)
                {
                    throw _operation.OperationException ?? new InvalidOperationException("资源更新操作失败");
                }

                switch (Stage)
                {
                    case GameUpdateStage.InitializingResources:
                        ReleaseOperation();
                        Track(Addressables.CheckForCatalogUpdates(false));
                        SetStage(GameUpdateStage.CheckingCatalogs);
                        break;
                    case GameUpdateStage.CheckingCatalogs:
                        _catalogs = new List<string>(_operation.Convert<List<string>>().Result);
                        ReleaseOperation();
                        if (_catalogs.Count > 0)
                        {
                            HotUpdateRequired = true;
                            Track(Addressables.UpdateCatalogs(_catalogs, false));
                            SetStage(GameUpdateStage.UpdatingCatalogs);
                        }
                        else
                        {
                            CheckDownloadSize();
                        }
                        break;
                    case GameUpdateStage.UpdatingCatalogs:
                        ReleaseOperation();
                        CheckDownloadSize();
                        break;
                    case GameUpdateStage.CheckingDownloadSize:
                        DownloadBytes = _operation.Convert<long>().Result;
                        ReleaseOperation();
                        if (DownloadBytes > 0)
                        {
                            HotUpdateRequired = true;
                            SetStage(GameUpdateStage.WaitingForHotUpdate);
                        }
                        else
                        {
                            Complete();
                        }
                        break;
                    case GameUpdateStage.Downloading:
                        ReleaseOperation();
                        DownloadedBytes = DownloadBytes;
                        DownloadProgress = 1;
                        Complete();
                        break;
                }
            }
            catch (Exception exception)
            {
                Fail(exception);
            }
        }

        private void TickSimulation()
        {
            if (Stage == GameUpdateStage.CheckingVersion)
            {
                _simulationProgress += Time.deltaTime;
                if (_simulationProgress < 1f)
                {
                    return;
                }

                int result = UnityEngine.Random.Range(0, 100);
                VersionInfo = new GameUpdateVersionInfo
                {
                    MinimumClientVersion = "0.0.0",
                    LatestClientVersion = "1.0.1",
                    InstallUrl = "https://example.com/update",
                    Notice = "发现新版本"
                };

                if (result < 25)
                {
                    HotUpdateRequired = true;
                    SetStage(GameUpdateStage.WaitingForHotUpdate);
                }
                else if (result < 50)
                {
                    ForceUpdateRequired = true;
                    SetStage(GameUpdateStage.WaitingForForceUpdate);
                }
                else
                {
                    Complete();
                }

                return;
            }

            if (Stage == GameUpdateStage.WaitingForHotUpdate && _downloadConfirmed)
            {
                _simulationProgress = 0f;
                SetStage(GameUpdateStage.Downloading);
                return;
            }

            if (Stage == GameUpdateStage.Downloading)
            {
                _simulationProgress = Mathf.Min(1f, _simulationProgress + Time.deltaTime / 3f);
                DownloadBytes = 1024 * 1024 * 20;
                DownloadedBytes = (long)(DownloadBytes * _simulationProgress);
                DownloadProgress = _simulationProgress;
                Notify(ProgressChanged);
                if (_simulationProgress >= 1f)
                {
                    Complete();
                }
            }
        }

        private void CheckVersion()
        {
            if (!_request.isDone)
            {
                return;
            }

            if (_request.result != UnityWebRequest.Result.Success)
            {
                throw new InvalidOperationException($"版本检查失败：HTTP {_request.responseCode}，{_request.error}");
            }

            VersionInfo = JsonUtility.FromJson<GameUpdateVersionInfo>(_request.downloadHandler.text);
            _request.Dispose();
            _request = null;
            if (VersionInfo == null)
            {
                throw new InvalidOperationException("版本接口返回空数据");
            }

            var client = ParseVersion(string.IsNullOrEmpty(_settings.ClientVersion)
                ? Application.version : _settings.ClientVersion);
            var minimum = ParseVersion(VersionInfo.MinimumClientVersion);
            if (!string.IsNullOrEmpty(VersionInfo.LatestClientVersion)
                && ParseVersion(VersionInfo.LatestClientVersion) < minimum)
            {
                throw new InvalidOperationException("最新客户端版本不能低于最低可用版本");
            }

            ForceUpdateRequired = client < minimum;
            if (ForceUpdateRequired)
            {
                RequireWebUrl(VersionInfo.InstallUrl);
                SetStage(GameUpdateStage.WaitingForForceUpdate);
                return;
            }

            Track(Addressables.InitializeAsync(false));
            SetStage(GameUpdateStage.InitializingResources);
        }

        private void CheckDownloadSize()
        {
            if (_labels.Length == 0)
            {
                Complete();
                return;
            }

            Track(Addressables.GetDownloadSizeAsync((System.Collections.IEnumerable)_labels));
            SetStage(GameUpdateStage.CheckingDownloadSize);
        }

        private void Track(AsyncOperationHandle operation)
        {
            _operation = operation;
            _hasOperation = true;
        }

        private void ReleaseOperation()
        {
            if (_hasOperation)
            {
                _hasOperation = false;
                if (_operation.IsValid())
                {
                    Addressables.Release(_operation);
                }
                _operation = default;
            }
        }

        private void Cleanup()
        {
            try
            {
                if (_request != null)
                {
                    try
                    {
                        _request.Abort();
                    }
                    finally
                    {
                        _request.Dispose();
                        _request = null;
                    }
                }
            }
            finally
            {
                // Addressables 释放所有权不等于取消底层下载；不再消费其结果。
                ReleaseOperation();
            }
        }

        private void Complete()
        {
            SetStage(GameUpdateStage.Complete);
            if (Stage == GameUpdateStage.Complete)
            {
                Notify(Completed);
            }
        }

        private void Fail(Exception exception)
        {
            ErrorMessage = exception.Message;
            Log.Error("[GameUpdateFlow] 更新失败", exception);
            try
            {
                Cleanup();
            }
            catch (Exception cleanupException)
            {
                Log.Error("[GameUpdateFlow] 清理更新操作失败", cleanupException);
            }
            SetStage(GameUpdateStage.Failed);
            if (Stage == GameUpdateStage.Failed)
            {
                Notify(Failed);
            }
        }

        private void SetStage(GameUpdateStage stage)
        {
            Stage = stage;
            Notify(StageChanged);
        }

        private static void Notify(Action handlers)
        {
            if (handlers == null)
            {
                return;
            }

            foreach (Action handler in handlers.GetInvocationList())
            {
                try
                {
                    handler();
                }
                catch (Exception exception)
                {
                    Log.Error("[GameUpdateFlow] 更新流程监听器异常", exception);
                }
            }
        }

        private static Version ParseVersion(string text)
        {
            var parts = (text ?? string.Empty).Split('.');
            var numbers = new int[4];
            if (parts.Length > 4)
            {
                throw new FormatException($"客户端版本格式无效：{text}");
            }

            for (int i = 0; i < parts.Length; i++)
            {
                if (!int.TryParse(parts[i], out numbers[i]) || numbers[i] < 0)
                {
                    throw new FormatException($"客户端版本格式无效：{text}");
                }
            }

            return new Version(numbers[0], numbers[1], numbers[2], numbers[3]);
        }

        private static void RequireWebUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                throw new ArgumentException($"更新地址必须为 HTTP/HTTPS 绝对地址：{url}");
            }
        }

        private sealed class UpdateState : FsmState<GameUpdateFlow>
        {
            protected override void OnEnter(IFsm<GameUpdateFlow> fsm)
            {
                fsm.Owner.Begin();
            }

            protected override void OnUpdate(IFsm<GameUpdateFlow> fsm, float deltaTime)
            {
                fsm.Owner.Tick();
            }

            protected override void OnLeave(IFsm<GameUpdateFlow> fsm, bool isShutdown)
            {
                fsm.Owner.Cleanup();
            }
        }
    }
}
