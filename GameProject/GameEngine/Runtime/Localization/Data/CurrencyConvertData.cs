using System.Collections.Generic;
using System.Globalization;

namespace GameEngine
{
    /// <summary>货币转换数据抽象基类，维护货币 id 到 <see cref="CurrencyEntry"/> 的映射。</summary>
    public abstract class CurrencyConvertData : LocalizationData<Currency>
    {
        private readonly Dictionary<uint, CurrencyEntry> _entries = new Dictionary<uint, CurrencyEntry>();

        /// <summary>已注册的货币转换数据数量。</summary>
        public int Count => _entries.Count;

        /// <summary>批量注册货币转换数据，重复 id 覆盖，非法条目跳过。</summary>
        /// <param name="entries">货币转换条目集合</param>
        public void AddCurrencies(IEnumerable<CurrencyEntry> entries)
        {
            if (entries == null)
            {
                Log.Error("[Currency] AddCurrencies failed: entries is null");
                return;
            }

            int total = 0;
            int success = 0;

            foreach (CurrencyEntry entry in entries)
            {
                total++;

                if (AddCurrency(entry))
                {
                    success++;
                }
            }

            Log.Debug($"[Currency] AddCurrencies 完成: total={total} success={success}");
        }

        /// <summary>注册单条货币转换数据。</summary>
        private bool AddCurrency(CurrencyEntry entry)
        {
            if (entry == null)
            {
                Log.Error("[Currency] AddCurrency failed: entry is null");
                return false;
            }

            if (!entry.IsValid())
            {
                Log.Error($"[Currency] AddCurrency failed: id={entry.Id} ratio={entry.Ratio} 非法");
                return false;
            }

            if (_entries.TryGetValue(entry.Id, out CurrencyEntry old))
            {
                Log.Warning($"[Currency] id={entry.Id} 已存在，旧条目被覆盖");
            }

            _entries[entry.Id] = entry;
            return true;
        }

        // ── 查询 ─────────────────────────────────────────────────────────────────

        /// <summary>获取指定 id 的显示金额，不存在时返回 0。</summary>
        /// <param name="id">货币 id</param>
        public virtual float GetMoney(uint id)
        {
            if (_entries.TryGetValue(id, out CurrencyEntry entry))
            {
                return entry.GetMoney();
            }

            Log.Warning($"[Currency] GetCurrency failed: id={id} not found");
            return 0f;
        }
    }
}
