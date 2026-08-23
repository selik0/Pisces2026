using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 精灵语言助手：静态持有当前使用的 <see cref="SpriteLanguageTable"/>（通过 <see cref="Initialize"/> 注入），
    /// 提供界面语言读写与按名称获取本地化精灵的能力。
    /// </summary>
    public static class SpriteUtility
    {
        private static SpriteLanguageTable _spriteLanguageTable;
        private static Language _language;

        /// <summary>初始化精灵语言表，通常由配置加载流程调用。</summary>
        /// <param name="spriteLanguageTable">精灵语言表实例</param>
        /// <exception cref="ArgumentNullException">spriteLanguageTable 为 null</exception>
        public static void Initialize(SpriteLanguageTable spriteLanguageTable)
        {
            if (spriteLanguageTable == null)
            {
                throw new ArgumentNullException(nameof(spriteLanguageTable), "[Sprite] SpriteUtility.Initialize failed: spriteLanguageTable is null");
            }

            _spriteLanguageTable = spriteLanguageTable;
            _spriteLanguageTable.SetLanguage(_language);
        }

        /// <summary>设置当前界面语言。</summary>
        /// <param name="language">界面语言</param>
        public static void SetLanguage(Language language)
        {
            _language = language;
            _spriteLanguageTable?.SetLanguage(language);
        }

        /// <summary>获取指定名称的本地化精灵（未完成实现，当前恒返回 null）。</summary>
        /// <param name="name">精灵名称</param>
        public static Sprite GetSprite(string name)
        {
            string realName = name;
            if (_spriteLanguageTable != null)
            {
                realName = _spriteLanguageTable.GetSpriteName(name);
            }

            return null;
        }
    }
}
