using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 单条音频剪辑描述，包含路径、随机权重与音量。
    /// </summary>
    [Serializable]
    public class AudioClipInfo
    {
        /// <summary>音效文件路径。</summary>
        public string Path = string.Empty;

        /// <summary>随机播放时的选中权重。</summary>
        public uint Weight = 1;

        /// <summary>音量。</summary>
        [Range(0f, 1f)]
        public float Volume = 1f;
    }
}
