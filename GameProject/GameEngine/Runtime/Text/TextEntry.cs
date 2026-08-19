namespace GameEngine
{
    /// <summary>
    /// 单条文本数据，对应文本表中一行配置。
    /// </summary>
    public sealed class TextEntry
    {
        /// <summary>文本 id，在同一文本表内唯一。</summary>
        public uint Id { get; }

        /// <summary>格式化所需参数数量，内容中以 {0} … {ParamCount - 1} 引用。</summary>
        public sbyte ParamCount { get; }

        /// <summary>文本内容，可包含 {0}、{1} 形式的格式化占位符。</summary>
        public string Content { get; }
        
        /// <summary>
        /// 使用指定 id、参数数量与文本内容构造文本条目。
        /// </summary>
        /// <param name="id">文本 id，在同一文本表内唯一</param>
        /// <param name="paramCount">格式化所需参数数量，内容中以 {0}、{1} … 引用</param>
        /// <param name="content">文本内容</param>
        public TextEntry(uint id, sbyte paramCount, string content)
        {
            Id = id;
            ParamCount = paramCount;
            Content = content;
        }

        // ── 判断 ─────────────────────────────────────────────────────────────────

        /// <summary>是否需要格式化参数，即内容中包含 {0} … 形式的占位符。</summary>
        public bool HasParams => ParamCount > 0;

        /// <summary>
        /// 判断条目字段是否合法，供配置加载方在注册前批量校验。
        /// </summary>
        /// <returns>参数数量非负且内容不为 null 时返回 true</returns>
        public bool IsValid()
        {
            return ParamCount >= 0 && Content != null;
        }
    }
}
