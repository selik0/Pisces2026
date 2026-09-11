using System;
using System.Collections.Generic;

namespace GameProto
{
    /// <summary>
    /// 配置表基类，统一使用 uint 主键管理配置记录及查询。
    /// </summary>
    /// <typeparam name="TConfig">配置记录类型。</typeparam>
    public abstract class ConfigTable<TKey, TConfig>
        where TKey : struct, IEquatable<TKey>
        where TConfig : ConfigRecord
    {
        private readonly Dictionary<TKey, TConfig> _items;

        protected ConfigTable()
        {
            _items = new Dictionary<TKey, TConfig>();
        }

        /// <summary>
        /// 获取配置记录数量。
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// 尝试按主键获取配置记录。
        /// </summary>
        public bool TryGet(TKey key, out TConfig config)
        {
            return _items.TryGetValue(key, out config);
        }

        /// <summary>
        /// 按主键获取配置记录，不存在时记录错误并返回 null。
        /// </summary>
        public TConfig Get(TKey key)
        {
            if (!_items.TryGetValue(key, out TConfig config))
            {
                ConfigLog.Error($"找不到主键为 {key} 的配置。");
                return null;
            }

            return config;
        }

        /// <summary>
        /// 添加配置记录，重复主键将被拒绝。
        /// </summary>
        protected void Add(TKey key, TConfig config)
        {
            if (config == null)
            {
                ConfigLog.Error("添加配置记录失败：config 不能为 null。");
                return;
            }

            if (_items.ContainsKey(key))
            {
                ConfigLog.Error($"配置主键重复：{key}。");
                return;
            }

            _items.Add(key, config);
        }
    }
}
