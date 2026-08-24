using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>精灵数据抽象基类，存储可多语言化的图片名称集合。</summary>
    public abstract class SpriteLanguageData : LocalizationData<Language>
    {
        protected HashSet<string> _spriteLanguages = new HashSet<string>();

        /// <summary>精灵语言标识数量。</summary>
        public int SpriteLanguageCount => _spriteLanguages.Count;

        protected string _spriteSuffix = string.Empty;

        /// <summary>获取本地化精灵名称，默认规则：名称后追加当前语言，如 name_en。</summary>
        public virtual string GetSpriteName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                Log.Warning("[Sprite] GetSpriteName failed: name is null or empty");
                return name;
            }
            if (string.IsNullOrEmpty(_spriteSuffix) || !_spriteLanguages.Contains(name))
            {
                return name;
            }
            return $"{name}_{_spriteSuffix}";
        }
    }
}
