using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace GameEngine
{
    /// <summary>文本管理器单例，内部持有 <see cref="TextLanguageData"/>，提供语言设置、按 id 获取/格式化文本与多语言字体更换。</summary>
    public sealed class TextManager : Singleton<TextManager>
    {
        private TextLanguageData _data;
        private FontLanguageConfig _fontConfig;
        private readonly Dictionary<string, Font> _fontCache = new Dictionary<string, Font>();

        /// <summary>当前界面语言对应的区域性信息。</summary>
        public static CultureInfo CultureInfo { get; private set; } = CultureInfoUtility.GetCultureInfo(Language.Chinese);

        /// <summary>切换界面语言后是否需要重启；为 false 时立即更新已注册字体。</summary>
        public bool IsNeedRestart { get; set; }

        // ── 本地化 ───────────────────────────────────────────────────────────────

        /// <summary>当前界面语言。</summary>
        public Language Language { get; private set; } = Language.Chinese;

        /// <summary>设置当前界面语言。</summary>
        public void SetLanguage(Language language)
        {
            if (Language == language)
            {
                return;
            }
            Language = language;
            CultureInfo = CultureInfoUtility.GetCultureInfo(language);
            _data?.SetLanguage(language);
            if (!IsNeedRestart)
            {
                TextUpdateRegistry.UpdateAll();
            }
        }

        /// <summary>初始化文本数据。</summary>
        /// <param name="data">文本数据</param>
        public void InitializeData(TextLanguageData data)
        {
            if (data == null)
            {
                Log.Error("[Text] InitializeData failed: data is null");
                return;
            }

            _data = data;
            _data.SetLanguage(Language);
            _data.InitializeData();
        }

        // ── 查询 ─────────────────────────────────────────────────────────────────

        /// <summary>已注册文本数量，未初始化时为 0。</summary>
        public int Count => _data?.Count ?? 0;

        /// <summary>获取指定 id 的文本，不存在时返回 "id=xx" 占位文本。</summary>
        public string GetText(uint id)
        {
            string text = _data?.GetText(id);
            return text ?? $"id={id}";
        }

        /// <summary>获取指定 id 的格式化文本，不存在时返回 "id=xx" 占位文本。</summary>
        public string GetText(uint id, params object[] args)
        {
            string text = _data?.GetText(id, args);
            return text ?? $"id={id}";
        }

        // ── 多语言字体 ───────────────────────────────────────────────────────────

        /// <summary>设置字体语言配置并刷新已注册文本。</summary>
        /// <param name="config">字体语言配置。</param>
        public void SetFontConfig(FontLanguageConfig config)
        {
            _fontConfig = config;
            TextUpdateRegistry.UpdateAll();
        }

        /// <summary>根据当前语言和初始字体为文本应用字体配置。</summary>
        /// <param name="text">需要更新的文本对象。</param>
        public void ApplyFont(IFontChange text)
        {
            if (text == null || string.IsNullOrEmpty(text.OriginalFontName))
            {
                Log.Warning("[Text] ApplyFont failed: text 或 text.OriginalFontName 为空");
                return;
            }

            if (_fontConfig == null)
            {
                Log.Warning("[Text] ApplyFont failed: FontConfig 未设置");
                return;
            }

            var entry = _fontConfig.GetEntry(Language.ToString(), text.OriginalFontName);
            if (entry == null)
            {
                Log.Warning($"[Text] ApplyFont failed: languageKey={Language} sourceFont={text.OriginalFontName} 未配置");
                return;
            }

            if (!_fontCache.TryGetValue(entry.TargetFontPath, out Font font))
            {
                font = Resources.Load<Font>(entry.TargetFontPath);
                if (font == null)
                {
                    Log.Warning($"[Text] ApplyFont failed: font={entry.TargetFontPath} 加载失败");
                    return;
                }
                _fontCache[entry.TargetFontPath] = font;
            }

            if (font.name != text.OriginalFontName)
            {
                text.ChangeFont(font, entry);
            }
        }
    }
}
