using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 场景切换时传递的参数容器。
    /// <para>
    /// 支持以字符串键存取任意类型的数据，供目标场景在 <see cref="IScene.OnEnter"/> 中读取。
    /// </para>
    /// <code>
    /// // 发起切换时构建参数
    /// var args = new SceneArgs()
    ///     .Set("level", 3)
    ///     .Set("fromMenu", true);
    ///
    /// SceneSystem.SwitchTo("GameScene", args);
    ///
    /// // 目标场景 OnEnter 中读取
    /// int level  = args.Get&lt;int&gt;("level");
    /// bool fromMenu = args.Get&lt;bool&gt;("fromMenu");
    /// </code>
    /// </summary>
    public sealed class SceneArgs
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();

        /// <summary>
        /// 设置一个参数值，支持链式调用。
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数键</param>
        /// <param name="value">参数值</param>
        /// <returns>自身，支持链式调用</returns>
        public SceneArgs Set<T>(string key, T value)
        {
            _data[key] = value;
            return this;
        }

        /// <summary>
        /// 获取一个参数值。键不存在时返回类型默认值。
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数键</param>
        /// <returns>参数值，不存在时返回 default(T)</returns>
        public T Get<T>(string key)
        {
            if (_data.TryGetValue(key, out var obj) && obj is T value)
                return value;
            return default;
        }

        /// <summary>
        /// 尝试获取一个参数值。
        /// </summary>
        /// <typeparam name="T">参数类型</typeparam>
        /// <param name="key">参数键</param>
        /// <param name="value">获取到的参数值</param>
        /// <returns>是否成功获取</returns>
        public bool TryGet<T>(string key, out T value)
        {
            if (_data.TryGetValue(key, out var obj) && obj is T v)
            {
                value = v;
                return true;
            }
            value = default;
            return false;
        }

        /// <summary>
        /// 是否包含指定键。
        /// </summary>
        public bool Has(string key) => _data.ContainsKey(key);

        /// <summary>
        /// 移除一个参数。
        /// </summary>
        public bool Remove(string key) => _data.Remove(key);

        /// <summary>
        /// 清空所有参数。
        /// </summary>
        public void Clear() => _data.Clear();
    }
}
