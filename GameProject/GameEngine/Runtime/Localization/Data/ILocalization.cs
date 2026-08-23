using System;

namespace GameEngine
{
    /// <summary>本地化接口，泛型参数为语言枚举类型。</summary>
    /// <typeparam name="TEnum">语言枚举类型</typeparam>
    public interface ILocalization<TEnum> where TEnum : Enum
    {
        /// <summary>是否已初始化。</summary>
        bool IsInitialized { get; }

        /// <summary>当前语言。</summary>
        TEnum Language { get; }

        /// <summary>设置当前语言。</summary>
        void SetLanguage(TEnum language);

        /// <summary>初始化本地化数据。</summary>
        void InitializeData();
    }
}
