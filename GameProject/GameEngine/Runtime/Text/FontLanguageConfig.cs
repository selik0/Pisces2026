using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>多语言字体配置，保存各语言的字体映射与显示比例。</summary>
    [CreateAssetMenu(fileName = "NewFontLanguageConfig", menuName = "GameEngine/FontLanguageConfig 多语言字体配置")]
    public class FontLanguageConfig : ScriptableObject
    {
        /// <summary>多语言字体配置列表。</summary>
        public FontLanguageEntry[] Entries = Array.Empty<FontLanguageEntry>();

        /// <summary>按语言 key 获取字体配置，不存在时返回 null。</summary>
        /// <param name="languageKey">语言 key</param>
        /// <param name="sourceFontName">来源字体名称</param>
        public FontLanguageEntry GetEntry(string languageKey, string sourceFontName)
        {
            if (Entries == null || Entries.Length == 0 || string.IsNullOrEmpty(languageKey) || string.IsNullOrEmpty(sourceFontName))
            {
                return null;
            }

            foreach (FontLanguageEntry entry in Entries)
            {
                if (entry == null)
                {
                    continue;
                }
                if (entry.LanguageKey == languageKey && entry.SourceFontName == sourceFontName)
                {
                    return entry;
                }
            }   

            return null;
        }
    }
}
