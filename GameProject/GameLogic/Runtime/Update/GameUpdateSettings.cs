namespace GameLogic
{
    /// <summary>启动更新配置；版本接口应按平台、渠道返回对应的 JSON。</summary>
    public sealed class GameUpdateSettings
    {
        public string VersionCheckUrl { get; set; }

        /// <summary>为空时使用 Application.version，格式为一至四段非负整数。</summary>
        public string ClientVersion { get; set; }

        public int RequestTimeoutSeconds { get; set; } = 30;

        /// <summary>需要预下载的 Addressables 标签；空数组表示只更新目录。</summary>
        public string[] DownloadLabels { get; set; } = System.Array.Empty<string>();
    }
}
