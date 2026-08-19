namespace GameEngine
{
    /// <summary>
    /// 音效播放模式。
    /// </summary>
    public enum AudioPlayMode
    {
        /// <summary>播放一次。</summary>
        PlayOnce,

        /// <summary>循环队列，按顺序播放。</summary>
        LoopSequence,

        /// <summary>循环队列，随机播放。</summary>
        LoopRandom,

        /// <summary>随机选择一次并循环播放。</summary>
        RandomOnceLoop,

        /// <summary>随机播放一次。</summary>
        PlayOnceRandom,
    }
}
