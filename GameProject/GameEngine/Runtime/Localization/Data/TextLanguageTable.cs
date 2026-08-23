using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 文本表抽象基类，实现 <see cref="ILocalization{TEnum}"/>（<see cref="GameEngine.Language"/>）。
    /// <para>
    /// 维护文本 id 到 <see cref="TextEntry"/> 的映射，提供按 id 获取文本、
    /// 带参数格式化文本的能力。本类不可直接实例化，具体表由派生类创建并持有，
    /// 文本数据通过 <see cref="AddTexts"/> 批量注册，本类不负责解析文本表文件，
    /// 也不提供移除接口。
    /// </para>
    ///
    /// <para><b>约定</b></para>
    /// <list type="bullet">
    ///   <item>id 重复注册时记录警告并以新条目覆盖</item>
    ///   <item>非法条目（参数数量为负或内容为 null）记录错误并跳过</item>
    ///   <item>带参获取时校验参数数量与 <see cref="TextEntry.ParamCount"/> 是否一致</item>
    ///   <item>格式化失败（占位符语法错误或越界）时记录异常并返回未格式化原文</item>
    /// </list>
    /// </summary>
    public abstract class TextLanguageTable : ILocalization<Language>
    {
        // ── 状态 ─────────────────────────────────────────────────────────────────

        private readonly Dictionary<uint, TextEntry> _texts = new Dictionary<uint, TextEntry>();

        /// <summary>当前已注册的文本数量。</summary>
        public int Count => _texts.Count;

        // ── 本地化 ───────────────────────────────────────────────────────────────

        /// <summary>当前界面语言。</summary>
        public Language Language { get; protected set; }

        /// <summary>设置当前界面语言。</summary>
        /// <param name="language">界面语言</param>
        public void SetLanguage(Language language)
        {
            Language = language;
            InitializeData();
        }

        /// <summary>初始化表数据，子类可重写以加载各自的数据。</summary>
        public abstract void InitializeData();

        // ── 注册 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 批量注册文本，通常由配置加载流程调用。
        /// <para>id 重复时记录警告并以新条目覆盖，非法条目记录错误并跳过。</para>
        /// </summary>
        /// <param name="entries">文本条目集合，不可为 null</param>
        public void AddTexts(IEnumerable<TextEntry> entries)
        {
            if (entries == null)
            {
                Log.Error("[Text] AddTexts failed: entries is null");
                return;
            }

            int total = 0;
            int success = 0;

            foreach (TextEntry entry in entries)
            {
                total++;

                if (AddText(entry))
                {
                    success++;
                }
            }

            Log.Debug($"[Text] AddTexts 完成: total={total} success={success}");
        }

        /// <summary>注册单条文本，供 <see cref="AddTexts"/> 内部复用。</summary>
        private bool AddText(TextEntry entry)
        {
            if (entry == null)
            {
                Log.Error("[Text] AddText failed: entry is null");
                return false;
            }

            if (entry.ParamCount < 0)
            {
                Log.Error($"[Text] AddText failed: id={entry.Id} paramCount={entry.ParamCount} 非法");
                return false;
            }

            if (entry.Content == null)
            {
                Log.Error($"[Text] AddText failed: id={entry.Id} content is null");
                return false;
            }

            if (_texts.TryGetValue(entry.Id, out TextEntry old))
            {
                Log.Warning($"[Text] id={entry.Id} 已存在，旧内容 \"{old.Content}\" 被覆盖");
            }

            _texts[entry.Id] = entry;
            return true;
        }

        // ── 查询 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 获取指定 id 的文本原文，不做格式化。
        /// </summary>
        /// <param name="id">文本 id</param>
        /// <returns>文本内容；不存在时记录警告并返回 null</returns>
        public string GetText(uint id)
        {
            if (_texts.TryGetValue(id, out TextEntry entry))
            {
                return entry.Content;
            }

            Log.Warning($"[Text] GetText failed: id={id} not found");
            return null;
        }

        /// <summary>
        /// 获取指定 id 的文本并用参数格式化。
        /// <para>
        /// 参数数量与 <see cref="TextEntry.ParamCount"/> 不一致时记录警告，
        /// 并继续尝试格式化；格式化抛出异常时返回未格式化原文。
        /// </para>
        /// </summary>
        /// <param name="id">文本 id</param>
        /// <param name="args">格式化参数，对应内容中的 {0}、{1} … 占位符</param>
        /// <returns>格式化后的文本；id 不存在时返回 null</returns>
        public string GetText(uint id, params object[] args)
        {
            if (!_texts.TryGetValue(id, out TextEntry entry))
            {
                Log.Warning($"[Text] GetText failed: id={id} not found");
                return null;
            }

            if (args == null || args.Length == 0)
            {
                if (entry.HasParams)
                {
                    Log.Warning($"[Text] id={id} 需要 {entry.ParamCount} 个参数但未传入，返回未格式化文本");
                }

                return entry.Content;
            }

            if (args.Length != entry.ParamCount)
            {
                Log.Warning($"[Text] id={id} 参数数量不匹配: 需要 {entry.ParamCount} 个, 传入 {args.Length} 个");
            }

            try
            {
                return string.Format(entry.Content, args);
            }
            catch (FormatException ex)
            {
                Log.Error($"[Text] id={id} 格式化失败: \"{entry.Content}\"", ex);
                return entry.Content;
            }
        }
    }
}
