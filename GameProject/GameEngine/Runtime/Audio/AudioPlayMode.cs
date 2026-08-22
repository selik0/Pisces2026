namespace GameEngine
{
    /// <summary>
    /// 音效播放模式。
    /// </summary>
    public enum AudioPlayMode
    {
        /// <summary>顺序播放一个AudioClip。</summary>
        Once,

        /// <summary>随机播放一个AudioClip。</summary>
        OnceRandom,

        /// <summary>按AudioClip列表顺序循环播放一次。</summary>
        OnceSequence,

        /// <summary>随机一次播放顺序并播放。</summary>
        OnceRandomSequence,

        /// <summary>按AudioClip列表顺序循环播放。</summary>
        SequenceLoop,

        /// <summary>随机选择一次播放顺序并循环播放。</summary>
        OnceRandomSequenceLoop,

        /// <summary>循环播放，每次随机选择一个AudioClip。</summary>
        RandomLoop,
    }
}
