using System;
using System.IO;
using System.Text;
using UnityEngine;

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
        public AudioLanguage AudioLanguage { get; private set; } = AudioLanguage.Chinese;

        /// <summary>当前结算货币。</summary>
        public Currency Currency { get; private set; } = Currency.CNY;

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
        public void SetAudioLanguage(AudioLanguage audioLanguage)
        {
            AudioLanguage = audioLanguage;
        }

        /// <summary>外部设置结算货币。</summary>
        public void SetCurrency(Currency currency)
        {
            Currency = currency;
        }

        // ── 本地配置读取 ────────────────────────────────────────────────────────

        /// <summary>
        /// 从本地 JSON 配置文件读取指定类型。
        /// 编辑器读取 EditorData 目录，其他平台读取 PersistentRoot 目录。
        /// </summary>
        /// <typeparam name="T">目标类型，需标记 <see cref="SerializableAttribute"/> 且字段可被 JsonUtility 序列化</typeparam>
        /// <param name="path">文件完整路径，例如 "Localization/config.json"</param>
        /// <returns>反序列化结果；文件不存在或解析失败时返回 default(T)</returns>
        public T ReadConfig<T>(string path)
        {
            if (!File.Exists(path))
            {
                Log.Warning($"[Localization] 本地配置文件不存在: {path}");
                return default(T);
            }

            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                return JsonUtility.FromJson<T>(json);
            }
            catch (Exception e)
            {
                Log.Error($"[Localization] 读取本地配置失败: {path}", e);
                return default(T);
            }
        }

        /// <summary>读取本地化配置并应用到当前管理器。</summary>
        /// <param name="relativePath">相对根目录的路径，例如 "Localization/config.json"</param>
        public void LoadConfig(string relativePath)
        {
            string root = Application.isEditor ? GameNative.FileSystem.EditorDataPath : GameNative.FileSystem.PersistentRoot;
            var path = Path.Combine(root, relativePath);
            LocalizationConfig config = ReadConfig<LocalizationConfig>(path);
            Apply(config);
        }

        /// <summary>将本地化配置应用到当前管理器，空字段保持原值。</summary>
        public void Apply(LocalizationConfig config)
        {
            if (config == null)
            {
                Log.Warning("[Localization] 应用本地化配置失败：config 为空");
                return;
            }

            if (EnumUtility.TryParseEnum(config.Region, out Region region))
            {
                SetRegion(region);
            }

            if (EnumUtility.TryParseEnum(config.Language, out Language language))
            {
                SetLanguage(language);
            }

            if (EnumUtility.TryParseEnum(config.AudioLanguage, out AudioLanguage audioLanguage))
            {
                SetAudioLanguage(audioLanguage);
            }

            if (EnumUtility.TryParseEnum(config.Currency, out Currency currency))
            {
                SetCurrency(currency);
            }
        }
    }
}
