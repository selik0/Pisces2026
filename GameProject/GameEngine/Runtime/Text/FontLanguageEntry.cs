using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>单条多语言字体配置。</summary>
    [Serializable]
    public sealed class FontLanguageEntry
    {
        /// <summary>语言 key，如 "Chinese"。</summary>
        public string LanguageKey;
#if GAME_DEBUG
        /// <summary>来源字体。</summary>
        public Font SourceFont;

        /// <summary>目标字体。</summary>
        public Font TargetFont;
#endif
        /// <summary>来源字体名称，由来源字体赋值时自动填充，不在 Inspector 显示。</summary>
        [HideInInspector] public string SourceFontName;

        /// <summary>目标字体路径，由目标字体赋值时自动填充，不在 Inspector 显示。</summary>
        [HideInInspector] public string TargetFontPath;

        /// <summary>字符大小缩放比例。</summary>
        public float SizeScale = 1f;

        /// <summary>行高比例。</summary>
        public float LineHeightRatio = 1f;
    }
}
