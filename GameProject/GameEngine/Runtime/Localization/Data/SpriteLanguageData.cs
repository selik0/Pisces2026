using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>精灵数据抽象基类，存储可多语言化的图片名称集合。</summary>
    public abstract class SpriteLanguageData : LocalizationData<Language>
    {
        protected HashSet<string> _spriteLanguages = new HashSet<string>();

        /// <summary>精灵语言标识数量。</summary>
        public int SpriteLanguageCount => _spriteLanguages.Count;

        /// <summary>获取指定名称的本地化精灵名称。</summary>
        public abstract string GetSpriteName(string name);
    }
}
