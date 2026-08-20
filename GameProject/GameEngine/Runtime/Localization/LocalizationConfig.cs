using System;

namespace GameEngine
{
    /// <summary>
    /// 本地化本地配置文件结构。
    /// 字段使用字符串保存枚举名称，便于人工编辑 JSON；应用时由 LocalizationManager 解析为对应枚举。
    /// </summary>
    [Serializable]
    public sealed class LocalizationConfig
    {
        /// <summary>发行地区名称，对应 <see cref="Region"/>，如 "China"。</summary>
        public string region;

        /// <summary>界面语言名称，对应 <see cref="Language"/>，如 "Chinese"。</summary>
        public string language;

        /// <summary>音频语言名称，对应 <see cref="Language"/>，如 "Chinese"。</summary>
        public string audioLanguage;

        /// <summary>结算货币代码，对应 <see cref="Currency"/>，如 "CNY"。</summary>
        public string currency;
    }
}
