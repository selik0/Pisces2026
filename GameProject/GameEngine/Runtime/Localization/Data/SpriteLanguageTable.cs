using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 存储可以多语言化的图片名称集合。
    /// </summary>
    public abstract class SpriteLanguageTable : ILocalization<Language>
    {
        protected HashSet<string> _spriteLanguages = new HashSet<string>();

        public Language Language { get; private set; }

        /// <summary>当前精灵语言标识数量。</summary>
        public int SpriteLanguageCount => _spriteLanguages.Count;

        public void SetLanguage(Language language)
        {
            Language = language;
            InitializeData();
        }

        public abstract void InitializeData();

        public abstract string GetSpriteName(string name);
    }
}