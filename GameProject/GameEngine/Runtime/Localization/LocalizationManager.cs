namespace GameEngine
{
    /// <summary>本地化配置管理器，保存当前地区、界面语言、音频语言与结算货币。</summary>
    public sealed class LocalizationManager : Singleton<LocalizationManager>
    {
        /// <summary>当前发行地区。</summary>
        public Region Region { get; private set; } = Region.China;

        /// <summary>当前界面语言。</summary>
        public Language Language { get; private set; } = Language.Chinese;

        /// <summary>当前音频语言。</summary>
        public AudioLanguage AudioLanguage { get; private set; } = AudioLanguage.Chinese;

        /// <summary>当前结算货币。</summary>
        public Currency Currency { get; private set; } = Currency.CNY;

        public LocalizationManager()
        {
            LocalizationConfig config = LocalizationConfig.LoadConfig();
            if (config != null)
            {
                Apply(config);
            }
        }

        /// <summary>设置发行地区。</summary>
        public void SetRegion(Region region)
        {
            Region = region;
        }

        /// <summary>设置界面语言。</summary>
        public void SetLanguage(Language language)
        {
            Language = language;
            TextManager.Instance.SetLanguage(language);
            SpriteUtility.SetLanguage(language);
        }

        /// <summary>设置音频语言。</summary>
        public void SetAudioLanguage(AudioLanguage audioLanguage)
        {
            AudioLanguage = audioLanguage;
            AudioManager.Instance.SetLanguage(audioLanguage);
        }

        /// <summary>设置结算货币。</summary>
        public void SetCurrency(Currency currency)
        {
            Currency = currency;
            CurrencyUtility.SetLanguage(currency);
        }

        /// <summary>应用本地化配置。</summary>
        internal void Apply(LocalizationConfig config)
        {
            if (config == null)
            {
                Log.Warning("[Localization] 应用本地化配置失败：config 为空");
                return;
            }

            SetRegion(config.Region);
            TextManager.Instance.IsNeedRestart = config.IsNeedRestart;
            SetLanguage(config.Language);
            SetAudioLanguage(config.AudioLanguage);
            SetCurrency(config.Currency);
        }
    }
}
