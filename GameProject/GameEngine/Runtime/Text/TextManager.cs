using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 文本管理器单例，持有全局文本表（id → <see cref="TextEntry"/>），
    /// 提供注册、移除与按 id 获取/格式化文本的统一入口。
    /// 文本数据由外部配置加载流程注册；登录/登出时清空文本表，避免跨会话残留。
    /// 格式化逻辑由 <see cref="TextEntry.GetText(object[])"/> 承担。
    /// </summary>
    public sealed class TextManager : Singleton<TextManager>
    {
        private readonly Dictionary<uint, TextEntry> _texts = new Dictionary<uint, TextEntry>();

        /// <summary>当前已注册的文本数量。</summary>
        public int Count => _texts.Count;

        // ── 注册 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 注册单条文本。id 重复时记录警告并以新条目覆盖，非法条目记录错误并跳过。
        /// </summary>
        public void Register(TextEntry entry)
        {
            if (entry == null)
            {
                Log.Error("[Text] Register failed: entry is null");
                return;
            }

            if (!entry.IsValid())
            {
                Log.Error($"[Text] Register failed: id={entry.Id} paramCount={entry.ParamCount} 非法");
                return;
            }

            if (_texts.TryGetValue(entry.Id, out TextEntry old))
            {
                Log.Warning($"[Text] id={entry.Id} 已存在，旧内容 \"{old.Content}\" 被覆盖");
            }

            _texts[entry.Id] = entry;
        }

        /// <summary>批量注册文本，通常由配置加载流程调用。</summary>
        /// <param name="entries">文本条目集合，不可为 null</param>
        public void RegisterTexts(IEnumerable<TextEntry> entries)
        {
            if (entries == null)
            {
                Log.Error("[Text] RegisterTexts failed: entries is null");
                return;
            }

            int total = 0;
            foreach (TextEntry entry in entries)
            {
                total++;
                Register(entry);
            }

            Log.Debug($"[Text] RegisterTexts 完成: total={total} count={_texts.Count}");
        }

        // ── 移除 ─────────────────────────────────────────────────────────────────

        /// <summary>注销指定 id 的文本，不存在时返回 false。</summary>
        public bool Unregister(uint id)
        {
            if (_texts.Remove(id))
            {
                Log.Debug($"[Text] Unregister id={id}");
                return true;
            }

            return false;
        }

        /// <summary>清空全部文本。</summary>
        public void Clear()
        {
            _texts.Clear();
            Log.Debug("[Text] Clear");
        }

        // ── 查询 ─────────────────────────────────────────────────────────────────

        /// <summary>获取指定 id 的文本条目，不存在时记录警告并返回 null。</summary>
        public TextEntry GetEntry(uint id)
        {
            if (_texts.TryGetValue(id, out TextEntry entry))
            {
                return entry;
            }

            Log.Warning($"[Text] GetEntry failed: id={id} not found");
            return null;
        }

        /// <summary>获取指定 id 的文本原文，不存在时记录警告并返回 "id=xx" 占位文本（不返回 null）。</summary>
        public string GetText(uint id)
        {
            TextEntry entry = GetEntry(id);
            return entry != null ? entry.GetText() : $"id={id}";
        }

        /// <summary>获取指定 id 的文本并用参数格式化，不存在时记录警告并返回 "id=xx" 占位文本（不返回 null）。</summary>
        public string GetText(uint id, params object[] args)
        {
            TextEntry entry = GetEntry(id);
            return entry != null ? entry.GetText(args) : $"id={id}";
        }
    }
}
