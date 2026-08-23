using System;

namespace GameEngine
{
    /// <summary>
    /// 本地化接口，泛型参数为界面语言枚举类型。
    /// </summary>
    /// <typeparam name="TEnum">界面语言枚举类型</typeparam>
    public interface ILocalization<TEnum> where TEnum : Enum
    {
        bool IsInitialized { get; }
        
        /// <summary>当前界面语言，可直接读写。</summary>
        TEnum Language { get; }

        void SetLanguage(TEnum language);

        /// <summary>按默认路径初始化本地化配置数据。</summary>
        void InitializeData();
    }
}
