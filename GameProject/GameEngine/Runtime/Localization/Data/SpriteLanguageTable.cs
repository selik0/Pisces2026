using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 存储可以多语言化的图片名称集合。
    /// </summary>
    public abstract class SpriteLanguageTable : LocalizationData<Language>
    {
        protected HashSet<string> _spriteLanguages = new HashSet<string>();

        /// <summary>当前精灵语言标识数量。</summary>
        public int SpriteLanguageCount => _spriteLanguages.Count;

        public abstract string GetSpriteName(string name);
    }
}