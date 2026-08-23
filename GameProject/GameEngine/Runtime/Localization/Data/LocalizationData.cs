using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 本地化接口，泛型参数为界面语言枚举类型。
    /// </summary>
    /// <typeparam name="TEnum">界面语言枚举类型</typeparam>
    public abstract class LocalizationData<TEnum> : ILocalization<TEnum> where TEnum : Enum
    {
        public bool IsInitialized { get; protected set; } = false;

        /// <summary>当前界面语言，可直接读写。</summary>
        public TEnum Language { get; protected set; }

        public virtual void SetLanguage(TEnum language)
        {
            if (EqualityComparer<TEnum>.Default.Equals(language, Language))
            {
                return;
            }

            IsInitialized = false;
            Language = language;
            InitializeData();
        }

        /// <summary>按默认路径初始化本地化配置数据。</summary>
        public virtual void InitializeData()
        {
            if (!IsInitialized)
            {
                IsInitialized = true;
                InternalInitializeData();
            }
        }

        protected abstract void InternalInitializeData();
    }
}
