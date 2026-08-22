using System;
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
        public AudioPlayMode PlayMode = AudioPlayMode.Once;

        /// <summary>AudioMixerGroup 分组。</summary>
        public AudioMixType MixerGroup = AudioMixType.Master;

        /// <summary>音频剪辑列表。</summary>
        public AudioClipInfo[] ClipPaths = new AudioClipInfo[1];

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

        /// <summary>是否无限循环（OnceSequence 播一遍后结束，不属于循环）。</summary>
        public bool IsLoop =>
            PlayMode == AudioPlayMode.SequenceLoop ||
            PlayMode == AudioPlayMode.RandomLoop ||
            PlayMode == AudioPlayMode.OnceRandomSequenceLoop;

        /// <summary>是否随机选择剪辑（含随机顺序类模式）。</summary>
        public bool IsRandom =>
            PlayMode == AudioPlayMode.OnceRandom ||
            PlayMode == AudioPlayMode.RandomLoop ||
            PlayMode == AudioPlayMode.OnceRandomSequence ||
            PlayMode == AudioPlayMode.OnceRandomSequenceLoop;

        /// <summary>
        /// 判断是否包含剪辑列表。
        /// </summary>
        /// <returns>剪辑列表非 null 且非空时返回 true</returns>
        public bool HasClips()
        {
            return ClipPaths != null && ClipPaths.Length > 0;
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

        // ── 剪辑获取 ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 根据播放模式获取本次播放的剪辑序列：
        /// <see cref="AudioPlayMode.Once"/> / <see cref="AudioPlayMode.OnceRandom"/> 返回单个剪辑（顺序第一个 / 随机一个）；
        /// <see cref="AudioPlayMode.OnceSequence"/> / <see cref="AudioPlayMode.SequenceLoop"/> 返回全部（配置顺序）；
        /// <see cref="AudioPlayMode.OnceRandomSequence"/> / <see cref="AudioPlayMode.OnceRandomSequenceLoop"/> 返回全部（随机打乱顺序）；
        /// <see cref="AudioPlayMode.RandomLoop"/> 返回全部（每轮随机由播放实例执行）。
        /// 无剪辑时返回空数组。可用于预加载、调试展示等场景。
        /// </summary>
        public AudioClipInfo[] GetClipPaths()
        {
            if (!HasClips())
            {
                return Array.Empty<AudioClipInfo>();
            }

            switch (PlayMode)
            {
                case AudioPlayMode.Once:
                    return new[] { ClipPaths[0] };

                case AudioPlayMode.OnceRandom:
                    return new[] { PickRandomClip() };

                case AudioPlayMode.OnceRandomSequence:
                case AudioPlayMode.OnceRandomSequenceLoop:
                    return ShuffleClips();

                case AudioPlayMode.RandomLoop:
                case AudioPlayMode.OnceSequence:
                case AudioPlayMode.SequenceLoop:
                default:
                    return CloneClips();
            }
        }

        /// <summary>拷贝全部剪辑为数组。</summary>
        private AudioClipInfo[] CloneClips()
        {
            AudioClipInfo[] clips = new AudioClipInfo[ClipPaths.Length];
            for (int i = 0; i < ClipPaths.Length; i++)
            {
                clips[i] = ClipPaths[i];
            }

            return clips;
        }

        /// <summary>Fisher-Yates 洗牌，返回随机顺序的全部剪辑。</summary>
        private AudioClipInfo[] ShuffleClips()
        {
            AudioClipInfo[] clips = CloneClips();
            for (int i = clips.Length - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                AudioClipInfo tmp = clips[i];
                clips[i] = clips[j];
                clips[j] = tmp;
            }

            return clips;
        }

        /// <summary>按剪辑权重随机选择一个条目；权重全为 0 时取第一个。</summary>
        private AudioClipInfo PickRandomClip()
        {
            long totalWeight = 0;
            for (int i = 0; i < ClipPaths.Length; i++)
            {
                if (ClipPaths[i] != null)
                {
                    totalWeight += ClipPaths[i].Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return ClipPaths[0];
            }

            long roll = (long)UnityEngine.Random.Range(0f, (float)totalWeight);
            for (int i = 0; i < ClipPaths.Length; i++)
            {
                AudioClipInfo clip = ClipPaths[i];
                if (clip == null || clip.Weight <= 0)
                {
                    continue;
                }

                roll -= clip.Weight;
                if (roll < 0)
                {
                    return clip;
                }
            }

            return ClipPaths[0];
        }
    }
}
