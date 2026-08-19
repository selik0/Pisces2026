using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 音频节点配置资产，描述一个音频实体的播放参数与剪辑列表。
    /// </summary>
    [CreateAssetMenu(fileName = "NewAudioEntry", menuName = "GameEngine/AudioEntry 音频节点")]
    public class AudioEntry : ScriptableObject
    {
        /// <summary>音频 id，在同一音频表内唯一。</summary>
        public uint Id = 0;

        /// <summary>音量随机区间，x 为下限，y 为上限。</summary>
        public Vector2 Volume = Vector2.one;

        /// <summary>音调随机区间，x 为下限，y 为上限。</summary>
        public Vector2 Pitch = Vector2.one;

        /// <summary>播放模式。</summary>
        public AudioPlayMode PlayMode = AudioPlayMode.PlayOnce;

        /// <summary>AudioMixerGroup 分组。</summary>
        public AudioMixType MixerGroup = AudioMixType.Master;

        /// <summary>音频剪辑列表。</summary>
        public List<AudioClipInfo> ClipPaths = new List<AudioClipInfo>(1);

        /// <summary>播放结束后的停顿时间（秒）。</summary>
        [Range(0f, 10f)]
        public float EndPauseTime = 0f;

        /// <summary>同时播放次数上限。</summary>
        public int MaxPlayNumOnce = 1;

        /// <summary>淡出时间（秒）。</summary>
        [Range(0f, 10f)]
        public float DiscardTime = 0f;

        /// <summary>淡入时间（秒）。</summary>
        [Range(0f, 10f)]
        public float FadeInTime = 0f;

        /// <summary>音效层级，用于同层抢占策略。</summary>
        [Range(0, 4)]
        public int SoundHierarchy = 1;

        // ── 判断 ─────────────────────────────────────────────────────────────────

        /// <summary>是否为循环播放模式。</summary>
        public bool IsLoop =>
            PlayMode == AudioPlayMode.LoopSequence ||
            PlayMode == AudioPlayMode.LoopRandom ||
            PlayMode == AudioPlayMode.RandomOnceLoop;

        /// <summary>是否随机选择剪辑播放。</summary>
        public bool IsRandom =>
            PlayMode == AudioPlayMode.PlayOnceRandom ||
            PlayMode == AudioPlayMode.LoopRandom ||
            PlayMode == AudioPlayMode.RandomOnceLoop;

        /// <summary>
        /// 判断是否包含剪辑列表。
        /// </summary>
        /// <returns>剪辑列表非 null 且非空时返回 true</returns>
        public bool HasClips()
        {
            return ClipPaths != null && ClipPaths.Count > 0;
        }

        /// <summary>
        /// 判断当前配置是否可直接播放，供外部在触发播放前校验。
        /// </summary>
        /// <returns>包含剪辑且所有剪辑路径非空时返回 true</returns>
        public bool CanPlay()
        {
            if (!HasClips())
            {
                return false;
            }

            foreach (AudioClipInfo clip in ClipPaths)
            {
                if (clip == null || string.IsNullOrEmpty(clip.Path))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
