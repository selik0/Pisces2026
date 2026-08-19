namespace GameEngine
{
    /// <summary>
    /// 音频混音分组类型，对应 AudioMixerGroup。
    /// </summary>
    public enum AudioMixType
    {
        /// <summary>主分组。</summary>
        Master,

        /// <summary>背景音乐。</summary>
        Bgm,

        /// <summary>游戏音效。</summary>
        Sfx,

        /// <summary>界面音效。</summary>
        Ui,

        /// <summary>语音。</summary>
        Voice,
    }
}
