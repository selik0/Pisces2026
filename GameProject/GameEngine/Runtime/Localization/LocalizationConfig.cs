using System;
using System.IO;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 本地化本地配置文件结构。
    /// 对外属性直接使用枚举；JSON 中保存枚举名称字符串，便于人工编辑，
    /// 枚举与字符串之间的转换由 <see cref="ISerializationCallbackReceiver"/> 完成。
    /// </summary>
    [Serializable]
    internal sealed class LocalizationConfig : ISerializationCallbackReceiver
    {
        private const string relativePath = "LocalizationConfig.json";
        // ── JsonUtility 序列化的字符串字段（字段名即 JSON 键名，保持小写命名惯例）──

        /// <summary>JSON 中的发行地区名称，如 "China"。</summary>
        [SerializeField] private string region;

        /// <summary>JSON 中的界面语言名称，如 "Chinese"。</summary>
        [SerializeField] private string language;

        /// <summary>JSON 中的音频语言名称，如 "Chinese"。</summary>
        [SerializeField] private string audioLanguage;

        /// <summary>JSON 中的结算货币代码，如 "CNY"。</summary>
        [SerializeField] private string currency;

        // ── 枚举属性 ─────────────────────────────────────────────────────────────

        /// <summary>发行地区；JSON 中未配置或无法解析时为 null。</summary>
        public Region Region { get; private set; }

        /// <summary>界面语言；JSON 中未配置或无法解析时为 null。</summary>
        public Language Language { get; private set; }

        /// <summary>音频语言；JSON 中未配置或无法解析时为 null。</summary>
        public AudioLanguage AudioLanguage { get; private set; }

        /// <summary>结算货币；JSON 中未配置或无法解析时为 null。</summary>
        public Currency Currency { get; private set; }

        public LocalizationConfig()
        {
            Region = Region.China;
            Language = Language.Chinese;
            AudioLanguage = AudioLanguage.Chinese;
            Currency = Currency.CNY;

            region = Region.ToString();
            language = Language.ToString();
            audioLanguage = AudioLanguage.ToString();
            currency = Currency.ToString();
        }

        // ── 序列化回调 ───────────────────────────────────────────────────────────

        /// <summary>序列化前将枚举属性写回字符串字段。</summary>
        public void OnBeforeSerialize()
        {
            region = Region.ToString();
            language = Language.ToString();
            audioLanguage = AudioLanguage.ToString();
            currency = Currency.ToString();
        }

        /// <summary>反序列化后将字符串字段解析为枚举属性。</summary>
        public void OnAfterDeserialize()
        {
            if (EnumUtility.TryParseEnum(region, out Region _region))
            {
                Region = _region;
            }
            else
            {
                Log.Error($"[Localization] OnAfterDeserialize: region={region} 无法解析为 Region 枚举");
            }

            if (EnumUtility.TryParseEnum(language, out Language _language))
            {
                Language = _language;
            }
            else
            {
                Log.Error($"[Localization] OnAfterDeserialize: language={language} 无法解析为 Language 枚举");
            }

            if (EnumUtility.TryParseEnum(audioLanguage, out AudioLanguage _audioLanguage))
            {
                AudioLanguage = _audioLanguage;
            }
            else
            {
                Log.Error($"[Localization] OnAfterDeserialize: audioLanguage={audioLanguage} 无法解析为 AudioLanguage 枚举");
            }

            if (EnumUtility.TryParseEnum(currency, out Currency _currency))
            {
                Currency = _currency;
            }
            else
            {
                Log.Error($"[Localization] OnAfterDeserialize: currency={currency} 无法解析为 Currency 枚举");
            }
        }

        // ── 本地配置读取 ────────────────────────────────────────────────────────

        /// <summary>获取本地化配置文件所在根目录：编辑器使用 EditorData 目录，其他平台使用持久化目录。</summary>
        private static string GetConfigRoot()
        {
            return Application.isEditor ? GameNative.FileSystem.EditorDataPath : GameNative.FileSystem.PersistentRoot;
        }

        /// <summary>读取本地化配置并应用到当前管理器。</summary>
        public static LocalizationConfig LoadConfig()
        {
            string path = Path.Combine(GetConfigRoot(), relativePath);

            if (!GameNative.FileSystem.Exists(path))
            {
                Log.Warning($"[Localization] 本地配置文件不存在: {path}");
                return null;
            }

            try
            {
                LocalizationConfig config = GameNative.FileSystem.ReadJson<LocalizationConfig>(path);
                return config;
            }
            catch (Exception e)
            {
                Log.Error($"[Localization] 读取本地配置失败: {path}", e);
                return null;
            }
        }

        /// <summary>将本地化配置保存到本地 JSON 文件。</summary>
        /// <param name="config">要保存的配置，不可为 null</param>
        internal static void SaveConfig(LocalizationConfig config)
        {
            if (config == null)
            {
                Log.Warning("[Localization] 保存本地化配置失败：config 为空");
                return;
            }

            string path = Path.Combine(GetConfigRoot(), relativePath);

            try
            {
                GameNative.FileSystem.WriteJson(path, config);
            }
            catch (Exception e)
            {
                Log.Error($"[Localization] 写入本地配置失败: {path}", e);
            }
        }
    }
}
