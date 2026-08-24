using System;

namespace GameEngine
{
    /// <summary>单条货币转换数据。</summary>
    [Serializable]
    public sealed class CurrencyEntry
    {
        /// <summary>货币 id，如人民币为 68。</summary>
        public uint Id;

        /// <summary>货币类型，如 USD。</summary>
        public Currency CurrencyType;

        /// <summary>美分数值，如 999。</summary>
        public uint Cents;

        /// <summary>显示比例，如 100，显示金额需除以该值。</summary>
        public int Ratio;

        /// <summary>显示比例是否为正值。</summary>
        public bool IsValid()
        {
            return Ratio > 0;
        }

        /// <summary>获取显示金额，比例非法时返回 0。</summary>
        public float GetMoney()
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
