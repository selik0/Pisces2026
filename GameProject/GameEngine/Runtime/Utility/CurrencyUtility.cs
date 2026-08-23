using System;

namespace GameEngine
{
    /// <summary>货币助手，静态持有 <see cref="CurrencyConvertData"/>，提供货币类型读写与显示金额获取。</summary>
    public static class CurrencyUtility
    {
        private static CurrencyConvertData _data;

        /// <summary>当前货币类型。</summary>
        public static Currency Language { get; private set; }

        /// <summary>已注册的货币转换数据数量，未初始化时为 0。</summary>
        public static int Count => _data?.Count ?? 0;

        /// <summary>初始化货币转换数据。</summary>
        /// <param name="data">货币转换数据</param>
        /// <exception cref="ArgumentNullException">data 为 null</exception>
        public static void Initialize(CurrencyConvertData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data), "[Currency] CurrencyUtility.Initialize failed: data is null");
            }

            _data = data;
            _data.SetLanguage(Language);
        }

        /// <summary>设置当前货币类型。</summary>
        public static void SetLanguage(Currency currency)
        {
            Language = currency;
            _data?.SetLanguage(currency);
        }

        /// <summary>获取指定 id 的显示金额，不存在时返回 0。</summary>
        public static float GetMoney(uint id)
        {
            if (_data == null)
            {
                Log.Error("[Currency] GetMoney failed: data is null");
                return 0f;
            }

            return _data.GetCurrency(id);
        }
    }
}
