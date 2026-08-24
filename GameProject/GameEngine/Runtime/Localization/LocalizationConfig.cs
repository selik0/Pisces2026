using System;
using System.IO;
using UnityEngine;

namespace GameEngine
{
    /// <summary>本地化本地配置文件结构：属性使用枚举，JSON 保存枚举名称字符串。</summary>
    [Serializable]
    internal sealed class LocalizationConfig : ISerializationCallbackReceiver
    {
        private const string relativePath = "LocalizationConfig.json";

        // ── JSON 序列化字段（字段名即 JSON 键名）──

        /// <summary>发行地区名称，如 "China"。</summary>
        [SerializeField] private string region;

        /// <summary>界面语言名称，如 "Chinese"。</summary>
        [SerializeField] private string language;

        /// <summary>音频语言名称，如 "Chinese"。</summary>
        [SerializeField] private string audioLanguage;

        /// <summary>结算货币代码，如 "CNY"。</summary>
        [SerializeField] private string currency;

        /// <summary>界面语言变化后是否需要提示用户重启客户端。</summary>
        [SerializeField] private bool isNeedRestart;

        // ── 枚举属性 ─────────────────────────────────────────────────────────────

        /// <summary>发行地区。</summary>
        public Region Region { get; private set; }

        /// <summary>界面语言。</summary>
        public Language Language { get; private set; }

        /// <summary>音频语言。</summary>
        public AudioLanguage AudioLanguage { get; private set; }

        /// <summary>结算货币。</summary>
        public Currency Currency { get; private set; }

        /// <summary>界面语言变化后是否标记客户端需要重启。</summary>
        public bool IsNeedRestart => isNeedRestart;

        public LocalizationConfig()
        {
            Region = Region.China;
            Language = Language.Chinese;
            AudioLanguage = AudioLanguage.Chinese;
            Currency = Currency.CNY;
            isNeedRestart = false;

            region = Region.ToString();
            language = Language.ToString();
            audioLanguage = AudioLanguage.ToString();
            currency = Currency.ToString();
        }

        // ── 序列化回调 ───────────────────────────────────────────────────────────

        /// <summary>序列化前：枚举写回字符串。</summary>
        public void OnBeforeSerialize()
        {
            region = Region.ToString();
            language = Language.ToString();
            audioLanguage = AudioLanguage.ToString();
            currency = Currency.ToString();
        }

        /// <summary>反序列化后：字符串解析为枚举。</summary>
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

        /// <summary>本地化配置文件根目录（编辑器用 EditorData，其他平台用持久化目录）。</summary>
        private static string GetConfigRoot()
        {
            return Application.isEditor ? GameNative.FileSystem.EditorDataPath : GameNative.FileSystem.PersistentRoot;
        }

        /// <summary>读取本地化配置，文件不存在或失败时返回 null。</summary>
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
                return GameNative.FileSystem.ReadJson<LocalizationConfig>(path);
            }
            catch (Exception e)
            {
                Log.Error($"[Localization] 读取本地配置失败: {path}", e);
                return null;
            }
        }

        /// <summary>保存本地化配置到本地 JSON。</summary>
        /// <param name="config">配置，不可为 null</param>
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
