namespace GameEngine
{
    /// <summary>
    /// 本地化配置管理器，保存当前地区、界面语言与音频语言。
    /// 地区与语言由外部登录、设置界面或渠道配置写入，读取方通过属性获取当前生效值。
    /// </summary>
    public sealed class LocalizationManager : Singleton<LocalizationManager>
    {
        /// <summary>当前发行地区。</summary>
        public Region Region { get; private set; } = Region.China;

        /// <summary>当前界面语言。</summary>
        public Language Language { get; private set; } = Language.Chinese;

        /// <summary>当前音频语言。</summary>
        public Language AudioLanguage { get; private set; } = Language.Chinese;

        public LocalizationManager()
        {
        }

        /// <summary>外部设置发行地区。</summary>
        public void SetRegion(Region region)
        {
            Region = region;
        }

        /// <summary>外部设置界面语言。</summary>
        public void SetLanguage(Language language)
        {
            Language = language;
        }

        /// <summary>外部设置音频语言。</summary>
        public void SetAudioLanguage(Language audioLanguage)
        {
            AudioLanguage = audioLanguage;
        }
    }
}
