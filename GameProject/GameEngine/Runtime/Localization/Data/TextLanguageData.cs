using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>文本数据抽象基类，维护文本 id 到 <see cref="TextEntry"/> 的映射。</summary>
    public abstract class TextLanguageData : LocalizationData<Language>
    {
        private readonly Dictionary<uint, TextEntry> _texts = new Dictionary<uint, TextEntry>();

        /// <summary>已注册文本数量。</summary>
        public int Count => _texts.Count;

        /// <summary>批量注册文本，重复 id 覆盖，非法条目跳过。</summary>
        /// <param name="entries">文本条目集合</param>
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

        /// <summary>注册单条文本。</summary>
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

        /// <summary>获取指定 id 的文本原文，不存在时返回 null。</summary>
        /// <param name="id">文本 id</param>
        public string GetText(uint id)
        {
            if (_texts.TryGetValue(id, out TextEntry entry))
            {
                return entry.Content;
            }

            Log.Warning($"[Text] GetText failed: id={id} not found");
            return null;
        }

        /// <summary>获取带参数格式化的文本，不存在时返回 null。</summary>
        /// <param name="id">文本 id</param>
        /// <param name="args">格式化参数，对应 {0}、{1} … 占位符</param>
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
