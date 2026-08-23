namespace GameEngine
{
    /// <summary>文本管理器单例，内部持有 <see cref="TextLanguageData"/>，提供语言设置与按 id 获取/格式化文本。</summary>
    public sealed class TextManager : Singleton<TextManager>
    {
        private TextLanguageData _data;

        // ── 本地化 ───────────────────────────────────────────────────────────────

        /// <summary>当前界面语言。</summary>
        public Language Language { get; private set; }

        /// <summary>设置当前界面语言。</summary>
        public void SetLanguage(Language language)
        {
            Language = language;
            _data?.SetLanguage(language);
        }

        /// <summary>初始化文本数据。</summary>
        /// <param name="data">文本数据</param>
        public void InitializeData(TextLanguageData data)
        {
            if (data == null)
            {
                Log.Error("[Text] InitializeData failed: data is null");
                return;
            }

            _data = data;
            _data.SetLanguage(Language);
            _data.InitializeData();
        }

        // ── 查询 ─────────────────────────────────────────────────────────────────

        /// <summary>已注册文本数量，未初始化时为 0。</summary>
        public int Count => _data?.Count ?? 0;

        /// <summary>获取指定 id 的文本，不存在时返回 "id=xx" 占位文本。</summary>
        public string GetText(uint id)
        {
            string text = _data?.GetText(id);
            return text ?? $"id={id}";
        }

        /// <summary>获取指定 id 的格式化文本，不存在时返回 "id=xx" 占位文本。</summary>
        public string GetText(uint id, params object[] args)
        {
            string text = _data?.GetText(id, args);
            return text ?? $"id={id}";
        }
    }
}
