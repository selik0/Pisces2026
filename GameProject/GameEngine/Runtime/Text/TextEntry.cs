using System;

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

        // ── 获取 ─────────────────────────────────────────────────────────────────

        /// <summary>获取文本原文，内容为 null 时返回 "id=xx" 占位文本（不返回 null）。</summary>
        public string GetText()
        {
            return Content ?? $"id={Id}";
        }

        /// <summary>
        /// 获取带参数格式化的文本。
        /// <para>
        /// 参数数量与 <see cref="ParamCount"/> 不一致时记录警告并继续尝试格式化；
        /// 格式化抛出异常时返回未格式化原文。
        /// </para>
        /// </summary>
        /// <param name="args">格式化参数，对应内容中的 {0}、{1} … 占位符</param>
        public string GetText(params object[] args)
        {
            if (Content == null)
            {
                Log.Error($"[Text] id={Id} 内容为 null，返回占位文本");
                return $"id={Id}";
            }

            if (args == null || args.Length == 0)
            {
                if (HasParams)
                {
                    Log.Warning($"[Text] id={Id} 需要 {ParamCount} 个参数但未传入，返回未格式化文本");
                }

                return Content;
            }

            if (args.Length != ParamCount)
            {
                Log.Warning($"[Text] id={Id} 参数数量不匹配: 需要 {ParamCount} 个, 传入 {args.Length} 个");
            }

            try
            {
                return string.Format(Content, args);
            }
            catch (FormatException ex)
            {
                Log.Error($"[Text] id={Id} 格式化失败: \"{Content}\"", ex);
                return Content;
            }
        }
    }
}
