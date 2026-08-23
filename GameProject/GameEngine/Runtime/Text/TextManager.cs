namespace GameEngine
{
    /// <summary>
    /// 文本管理器单例，内部持有 <see cref="TextLanguageTable"/>（未初始化前为空），
    /// 通过 <see cref="InitializeData"/> 注入具体文本表，提供语言设置与按 id 获取/格式化文本的统一入口。
    /// 格式化逻辑由 <see cref="TextEntry.GetText(object[])"/> 承担。
    /// </summary>
    public sealed class TextManager : Singleton<TextManager>
    {
        private TextLanguageTable _table;

        // ── 本地化 ───────────────────────────────────────────────────────────────

        /// <summary>当前界面语言。</summary>
        public Language Language { get; private set; }

        /// <summary>设置当前界面语言。</summary>
        /// <param name="language">界面语言</param>
        public void SetLanguage(Language language)
        {
            Language = language;
            _table?.SetLanguage(language);
        }

        /// <summary>初始化文本表：将具体文本表赋给 Manager 并加载其数据。</summary>
        /// <param name="table">文本表，不可为 null</param>
        public void InitializeData(TextLanguageTable table)
        {
            if (table == null)
            {
                Log.Error("[Text] InitializeData failed: table is null");
                return;
            }

            _table = table;
            _table.InitializeData();
        }

        // ── 查询 ─────────────────────────────────────────────────────────────────

        /// <summary>当前文本表中已注册的文本数量，未初始化时为 0。</summary>
        public int Count => _table?.Count ?? 0;

        /// <summary>获取指定 id 的文本原文，不存在时记录警告并返回 "id=xx" 占位文本（不返回 null）。</summary>
        public string GetText(uint id)
        {
            string text = _table?.GetText(id);
            return text ?? $"id={id}";
        }

        /// <summary>获取指定 id 的文本并用参数格式化，不存在时记录警告并返回 "id=xx" 占位文本（不返回 null）。</summary>
        public string GetText(uint id, params object[] args)
        {
            string text = _table?.GetText(id, args);
            return text ?? $"id={id}";
        }
    }
}
