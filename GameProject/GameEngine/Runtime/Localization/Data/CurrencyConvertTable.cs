using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 货币转换表抽象基类，实现 <see cref="ILocalization{TEnum}"/>（<see cref="Currency"/>）。
    /// <para>
    /// 保存所有货币转换数据，维护货币 id 到 <see cref="CurrencyEntry"/> 的映射，
    /// 提供按 id 获取显示金额的能力。本类不可直接实例化，具体表由派生类创建并持有，
    /// 转换数据通过 <see cref="AddCurrencies"/> 批量注册，本类不负责解析转换表文件，
    /// 也不提供移除接口。
    /// </para>
    ///
    /// <para><b>约定</b></para>
    /// <list type="bullet">
    ///   <item>id 重复注册时记录警告并以新条目覆盖</item>
    ///   <item>非法条目（显示比例为非正值）记录错误并跳过</item>
    /// </list>
    /// </summary>
    public abstract class CurrencyConvertTable : ILocalization<Currency>
    {
        // ── 状态 ─────────────────────────────────────────────────────────────────

        private readonly Dictionary<uint, CurrencyEntry> _entries = new Dictionary<uint, CurrencyEntry>();

        /// <summary>当前已注册的货币转换数据数量。</summary>
        public int Count => _entries.Count;

        // ── 本地化 ───────────────────────────────────────────────────────────────

        /// <summary>当前货币类型。</summary>
        public Currency Language { get; protected set; }

        /// <summary>设置当前货币类型。</summary>
        /// <param name="currency">货币类型</param>
        public void SetLanguage(Currency currency)
        {
            Language = currency;
            InitializeData();
        }

        /// <summary>初始化表数据，子类可重写以加载各自的数据。</summary>
        public abstract void InitializeData();

        // ── 注册 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 批量注册货币转换数据，通常由配置加载流程调用。
        /// <para>id 重复时记录警告并以新条目覆盖，非法条目记录错误并跳过。</para>
        /// </summary>
        /// <param name="entries">货币转换条目集合，不可为 null</param>
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

        /// <summary>注册单条货币转换数据，供 <see cref="AddCurrencies"/> 内部复用。</summary>
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

        /// <summary>
        /// 获取指定 id 的货币转换条目。
        /// </summary>
        /// <param name="id">货币 id</param>
        /// <returns>货币转换条目；不存在时记录警告并返回 null</returns>
        public float GetCurrency(uint id)
        {
            if (_entries.TryGetValue(id, out CurrencyEntry entry))
            {
                return entry.GetDisplayAmount();
            }

            Log.Warning($"[Currency] GetCurrency failed: id={id} not found");
            return 0f;
        }
    }
}
