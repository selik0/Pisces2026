using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>本地化数据抽象基类，泛型参数为语言枚举类型。</summary>
    /// <typeparam name="TEnum">语言枚举类型</typeparam>
    public abstract class LocalizationData<TEnum> : ILocalization<TEnum> where TEnum : Enum
    {
        /// <summary>是否已初始化。</summary>
        public bool IsInitialized { get; protected set; } = false;

        /// <summary>当前语言。</summary>
        public TEnum Language { get; protected set; }

        /// <summary>设置当前语言，语言变化时重新初始化数据。</summary>
        public virtual void SetLanguage(TEnum language)
        {
            if (EqualityComparer<TEnum>.Default.Equals(language, Language) && IsInitialized)
            {
                return;
            }

            IsInitialized = false;
            Language = language;
            InitializeData();
        }

        /// <summary>初始化数据，子类通过 <see cref="InternalInitializeData"/> 提供实现。</summary>
        public virtual void InitializeData()
        {
            if (!IsInitialized)
            {
                IsInitialized = true;
                InternalInitializeData();
            }
        }

        /// <summary>子类实现的数据加载。</summary>
        protected abstract void InternalInitializeData();
    }
}
