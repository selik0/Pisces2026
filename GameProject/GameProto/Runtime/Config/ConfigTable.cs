using System;
using System.Collections.Generic;

namespace GameProto
{
    /// <summary>
    /// 配置表基类，统一使用 uint 主键管理配置记录及查询。
    /// </summary>
    /// <typeparam name="TConfig">配置记录类型。</typeparam>
    public abstract class ConfigTable<TConfig>
        where TConfig : ConfigRecord
    {
        private readonly Dictionary<uint, TConfig> _items;

        protected ConfigTable(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _items = new Dictionary<uint, TConfig>(capacity);
        }

        /// <summary>
        /// 获取配置记录数量。
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// 尝试按主键获取配置记录。
        /// </summary>
        public bool TryGet(uint key, out TConfig config)
        {
            return _items.TryGetValue(key, out config);
        }

        /// <summary>
        /// 按主键获取配置记录，不存在时抛出异常。
        /// </summary>
        public TConfig Get(uint key)
        {
            if (!_items.TryGetValue(key, out TConfig config))
            {
                throw new KeyNotFoundException($"找不到主键为 {key} 的配置。");
            }

            return config;
        }

        /// <summary>
        /// 添加配置记录，重复主键将被拒绝。
        /// </summary>
        protected void Add(uint key, TConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (_items.ContainsKey(key))
            {
                throw new ConfigSerializationException($"配置主键重复：{key}。");
            }

            _items.Add(key, config);
        }
    }
}
