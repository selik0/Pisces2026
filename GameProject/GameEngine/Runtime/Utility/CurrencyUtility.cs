using System;

namespace GameEngine
{
    /// <summary>
    /// 货币助手：静态持有当前使用的 <see cref="CurrencyConvertTable"/>（通过 <see cref="Initialize"/> 注入），
    /// 提供货币类型读写与按 id 获取显示金额的能力。
    /// </summary>
    public static class CurrencyUtility
    {
        private static CurrencyConvertTable _table;

        /// <summary>获取当前货币类型。</summary>
        public static Currency Language { get; private set; }

        /// <summary>获取当前已注册的货币转换数据数量，未初始化时为 0。</summary>
        public static int Count => _table?.Count ?? 0;

        /// <summary>初始化货币转换表，通常由配置加载流程调用。</summary>
        /// <param name="table">货币转换表实例</param>
        /// <exception cref="ArgumentNullException">table 为 null</exception>
        public static void Initialize(CurrencyConvertTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table), "[Currency] CurrencyUtility.Initialize failed: table is null");
            }

            _table = table;
            _table.SetLanguage(Language);
        }

        /// <summary>设置当前货币类型。</summary>
        /// <param name="currency">货币类型</param>
        public static void SetLanguage(Currency currency)
        {
            Language = currency;
            _table?.SetLanguage(currency);
        }

        /// <summary>获取指定 id 的显示金额，不存在时返回 0。</summary>
        /// <param name="id">货币 id</param>
        public static float GetMoney(uint id)
        {
            if (_table == null)
            {
                Log.Error("[Currency] GetMoney failed: table is null");
                return 0f;
            }

            return _table.GetCurrency(id);
        }
    }
}
