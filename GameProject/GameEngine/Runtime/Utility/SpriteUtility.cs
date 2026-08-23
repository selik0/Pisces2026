using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>精灵语言助手，静态持有 <see cref="SpriteLanguageData"/>，提供语言读写与本地化精灵获取。</summary>
    public static class SpriteUtility
    {
        private static SpriteLanguageData _spriteLanguageData;
        private static Language _language;

        /// <summary>初始化精灵语言数据。</summary>
        /// <param name="spriteLanguageData">精灵语言数据</param>
        /// <exception cref="ArgumentNullException">spriteLanguageData 为 null</exception>
        public static void Initialize(SpriteLanguageData spriteLanguageData)
        {
            if (spriteLanguageData == null)
            {
                throw new ArgumentNullException(nameof(spriteLanguageData), "[Sprite] SpriteUtility.Initialize failed: spriteLanguageData is null");
            }

            _spriteLanguageData = spriteLanguageData;
            _spriteLanguageData.SetLanguage(_language);
        }

        /// <summary>设置当前界面语言。</summary>
        public static void SetLanguage(Language language)
        {
            _language = language;
            _spriteLanguageData?.SetLanguage(language);
        }

        /// <summary>获取指定名称的本地化精灵（未实现）。</summary>
        /// <param name="name">精灵名称</param>
        public static Sprite GetSprite(string name)
        {
            string realName = name;
            if (_spriteLanguageData != null)
            {
                realName = _spriteLanguageData.GetSpriteName(name);
            }

            return null;
        }
    }
}
