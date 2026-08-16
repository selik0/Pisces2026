namespace GameEngine
{
    /// <summary>
    /// 全局本地化配置静态入口，保存当前地区、界面语言与音频语言。
    /// 地区与语言由外部登录、设置界面或渠道配置写入，读取方通过属性获取当前生效值。
    /// </summary>
    public static class LocalizationSystem
    {

        /// <summary>当前发行地区。</summary>
        public static Region Region { get; private set; } = Region.China;

        /// <summary>当前界面语言。</summary>
        public static Language Language { get; private set; } = Language.Chinese;
        /// <summary>当前音频语言。</summary>
        public static Language AudioLanguage { get; private set; } = Language.Chinese;

        /// <summary>外部设置发行地区。</summary>
        public static void SetRegion(Region region)
        {
            Region = region;
        }

        /// <summary>外部设置界面语言。</summary>
        public static void SetLanguage(Language language)
        {
            Language = language;
        }

        /// <summary>外部设置音频语言。</summary>
        public static void SetAudioLanguage(Language audioLanguage)
        {
            AudioLanguage = audioLanguage;
        }
    }
}
