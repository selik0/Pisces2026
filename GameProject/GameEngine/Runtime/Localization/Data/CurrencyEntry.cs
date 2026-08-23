using System;

namespace GameEngine
{
    /// <summary>
    /// 单条货币转换数据，对应货币转换表中一行配置。
    /// </summary>
    [Serializable]
    public sealed class CurrencyEntry
    {
        /// <summary>货币 id，如人民币为 68，在同一转换表内唯一。</summary>
        public uint Id;

        /// <summary>货币类型，如 USD。</summary>
        public Currency CurrencyType;

        /// <summary>转为美分的数值，如 999。</summary>
        public uint Cents;

        /// <summary>显示比例，如 100，表示显示金额之前要除以该值。</summary>
        public int Ratio;

        /// <summary>
        /// 判断条目字段是否合法，供配置加载方在注册前批量校验。
        /// </summary>
        /// <returns>显示比例为正值时返回 true</returns>
        public bool IsValid()
        {
            return Ratio > 0;
        }

        /// <summary>获取显示金额，即美分数值除以显示比例；比例非法时记录警告并返回 0。</summary>
        public float GetDisplayAmount()
        {
            if (!IsValid())
            {
                Log.Warning($"[Currency] id={Id} 显示比例非法: {Ratio}，无法计算显示金额");
                return 0f;
            }

            return (float)Cents / Ratio;
        }
    }
}
