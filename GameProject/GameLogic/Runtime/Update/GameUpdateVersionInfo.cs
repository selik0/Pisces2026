using System;

namespace GameLogic
{
    /// <summary>版本接口 JSON 数据。客户端低于 MinimumClientVersion 时必须安装新包。</summary>
    [Serializable]
    public sealed class GameUpdateVersionInfo
    {
        public string MinimumClientVersion;
        public string LatestClientVersion;
        public string InstallUrl;
        public string Notice;
    }
}
