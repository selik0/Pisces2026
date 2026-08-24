using System.Collections.Generic;
using GameEngine;

namespace GameLogic
{
    /// <summary>游戏精灵语言数据，继承 <see cref="SpriteLanguageData"/>。</summary>
    public sealed class GameSpriteLanguageData : SpriteLanguageData
    {
        private static Dictionary<Language, string> _languageSuffixes = new Dictionary<Language, string>
        {
            { Language.TraditionalChinese, "TraditionalChinese" },
            { Language.English, "English" },
            { Language.Japanese, "Japanese" },
            { Language.Korean, "Korean" },
            { Language.French, "French" },
            { Language.German, "German" },
            { Language.Spanish, "Spanish" },
            { Language.Arabic, "Arabic" },
            { Language.Thai, "Thai" },
            { Language.Indonesian, "Indonesian" },
            { Language.Turkish, "Turkish" },
        };

        public override void SetLanguage(Language language)
        {
            base.SetLanguage(language);
            _spriteSuffix = _languageSuffixes.TryGetValue(language, out string suffix) ? suffix : string.Empty;
        }


        /// <summary>加载精灵语言数据，当前暂无数据源。</summary>
        protected override void InternalInitializeData()
        {

        }
    }
}
