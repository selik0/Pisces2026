using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 音频播放实例状态。
    /// </summary>
    public enum SoundPlaybackState
    {
        /// <summary>正在播放（含淡入过程）。</summary>
        Playing,

        /// <summary>当前剪辑播放完毕，处于 <see cref="AudioEntry.EndPauseTime"/> 停顿中。</summary>
        Pausing,

        /// <summary>已请求停止，处于 <see cref="AudioEntry.DiscardTime"/> 淡出中。</summary>
        FadingOut,
    }

    /// <summary>
    /// 单个音频播放实例，负责按一个 <see cref="AudioEntry"/> 配置播放的完整状态机：
    /// 序列推进、淡入淡出、停顿与播完检测。
    /// 播放序列由 <see cref="AudioEntry.GetClipPaths"/> 按模式生成；是否循环与是否每轮随机
    /// 由外部（<see cref="AudioManager"/>）按条目播放模式传入。
    /// 实例只管理播放，不负责剪辑加载：当前剪辑播完后通过 <see cref="NeedsNextClip"/> 暴露换曲需求，
    /// 由 <see cref="AudioManager"/> 加载并调用 <see cref="Play"/> 完成换曲。
    /// </summary>
    public sealed class SoundInstance
    {
        /// <summary>当前播放模式下的播放序列（由 <see cref="AudioEntry.GetClipPaths"/> 按模式生成）。</summary>
        private readonly AudioClipInfo[] _clips;

        /// <summary>是否循环播放（序列播完后绕回开头）。</summary>
        private readonly bool _isLoop;

        /// <summary>是否每轮随机选择剪辑（RandomLoop 模式）。</summary>
        private readonly bool _isRandomLoop;

        /// <summary>当前在播放序列中的位置。</summary>
        private int _clipIndex;

        private float _entryVolume;
        private float _clipVolume;
        private float _volumeScale;
        private float _fadeInTime;
        private float _fadeInRemaining;
        private float _fadeOutRemaining;
        private float _endPauseTime;
        private float _endPauseRemaining;

        /// <summary>音频条目 Id。</summary>
        public uint EntryId { get; }

        /// <summary>绑定的音源。</summary>
        public AudioSource Source { get; private set; }

        /// <summary>混音分组。</summary>
        public AudioMixType Group { get; }

        /// <summary>音效层级，用于同层抢占。</summary>
        public int Hierarchy { get; }

        /// <summary>是否为 BGM 通道实例。</summary>
        public bool IsBgm { get; private set; }

        /// <summary>创建时刻，用于同层抢占时选择最旧实例。</summary>
        public float StartRealTime { get; }

        /// <summary>当前状态。</summary>
        public SoundPlaybackState State { get; private set; }

        /// <summary>停止时的淡出时长。</summary>
        public float DiscardTime { get; }

        /// <summary>当前正在播放的剪辑，未播放时为 null。</summary>
        public AudioClip CurrentClip => Source != null ? Source.clip : null;

        /// <summary>当前剪辑已播放完毕（含停顿结束），需要由外部加载下一个剪辑并换曲。</summary>
        public bool NeedsNextClip
        {
            get
            {
                if (State == SoundPlaybackState.Playing)
                {
                    return !Source.isPlaying;
                }

                if (State == SoundPlaybackState.Pausing)
                {
                    return _endPauseRemaining <= 0f;
                }

                return false;
            }
        }

        internal SoundInstance(AudioEntry entry, AudioSource source,
            bool isLoop, bool isRandomLoop, float volumeScale, bool isBgm)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _clips = entry.GetClipPaths();
            _isLoop = isLoop;
            _isRandomLoop = isRandomLoop;
            _clipIndex = 0;

            Source = source;
            IsBgm = isBgm;
            EntryId = entry.Id;
            Group = entry.MixerGroup;
            Hierarchy = entry.SoundHierarchy;
            DiscardTime = entry.DiscardTime;

            _entryVolume = RandomRange(entry.Volume);
            _volumeScale = volumeScale;
            _fadeInTime = entry.FadeInTime;
            _fadeInRemaining = entry.FadeInTime;
            _endPauseTime = entry.EndPauseTime;
            StartRealTime = Time.realtimeSinceStartup;
            State = SoundPlaybackState.Playing;

            Source.pitch = RandomRange(entry.Pitch);
        }

        /// <summary>设置并开始播放指定剪辑（首次播放与换曲均通过本方法，成功后回到播放状态）。</summary>
        /// <param name="clip">要播放的剪辑，不能为 null。</param>
        /// <param name="clipVolume">剪辑音量（<see cref="AudioClipInfo.Volume"/>）。</param>
        /// <param name="loop">是否循环当前剪辑；由播放状态机推进时应传 false。</param>
        /// <returns>是否成功开始播放。</returns>
        public bool Play(AudioClip clip, float clipVolume, bool loop)
        {
            if (clip == null)
            {
                Log.Error($"[SoundInstance] 播放失败：clip 为 null，id={EntryId}。");
                return false;
            }

            _clipVolume = clipVolume;
            Source.clip = clip;
            Source.loop = loop;
            Source.Play();
            State = SoundPlaybackState.Playing;
            return true;
        }

        /// <summary>请求停止：按 <see cref="DiscardTime"/> 淡出，无淡出配置时下一帧结束。</summary>
        public void RequestStop()
        {
            if (State == SoundPlaybackState.FadingOut)
            {
                return;
            }

            State = SoundPlaybackState.FadingOut;
            _fadeOutRemaining = DiscardTime;
        }

        /// <summary>每帧推进实例状态（淡入淡出、停顿、播完检测），由 <see cref="AudioManager"/> 调用。</summary>
        /// <param name="deltaTime">帧间隔（秒），建议使用不受时间缩放影响的帧间隔。</param>
        /// <returns>是否仍在播放；返回 false 表示淡出完成，可回收该实例。</returns>
        public bool Update(float deltaTime)
        {
            switch (State)
            {
                case SoundPlaybackState.Playing:
                {
                    if (_fadeInRemaining > 0f)
                    {
                        _fadeInRemaining = Mathf.Max(0f, _fadeInRemaining - deltaTime);
                    }

                    if (!Source.isPlaying && _endPauseTime > 0f)
                    {
                        State = SoundPlaybackState.Pausing;
                        _endPauseRemaining = _endPauseTime;
                    }

                    return true;
                }

                case SoundPlaybackState.Pausing:
                {
                    _endPauseRemaining -= deltaTime;
                    return true;
                }

                case SoundPlaybackState.FadingOut:
                {
                    if (_fadeOutRemaining > 0f)
                    {
                        _fadeOutRemaining = Mathf.Max(0f, _fadeOutRemaining - deltaTime);
                    }

                    return _fadeOutRemaining > 0f;
                }
            }

            return true;
        }

        /// <summary>
        /// 当前模式下初始播放的剪辑（供 <see cref="AudioManager"/> 首次加载）。
        /// 每轮随机模式随机选择，其余模式取序列第一个。
        /// </summary>
        internal AudioClipInfo InitialClip
        {
            get
            {
                if (_clips.Length == 0)
                {
                    return null;
                }

                if (_isRandomLoop)
                {
                    return PickRandomClip(_clips);
                }

                return _clips[0];
            }
        }

        /// <summary>
        /// 推进到下一个剪辑并返回其配置；一轮播放的最后一曲之后返回 null（表示播放结束）。
        /// 每轮随机模式随机选择，循环模式序列绕回开头。
        /// </summary>
        public AudioClipInfo MoveToNextClip()
        {
            if (_clips.Length == 0)
            {
                return null;
            }

            if (_isRandomLoop)
            {
                return PickRandomClip(_clips);
            }

            if (_isLoop)
            {
                _clipIndex = (_clipIndex + 1) % _clips.Length;
            }
            else
            {
                if (_clipIndex >= _clips.Length - 1)
                {
                    return null;
                }

                _clipIndex++;
            }

            return _clips[_clipIndex];
        }

        /// <summary>
        /// 按当前淡入淡出进度与外部音量参数重算音源音量。
        /// 由 <see cref="AudioManager"/> 在每帧推进后调用。
        /// </summary>
        public void ApplyVolume(float groupVolume, float masterVolume)
        {
            float fadeScale;
            if (State == SoundPlaybackState.FadingOut)
            {
                fadeScale = DiscardTime > 0f ? _fadeOutRemaining / DiscardTime : 0f;
            }
            else if (_fadeInTime > 0f)
            {
                fadeScale = 1f - _fadeInRemaining / _fadeInTime;
            }
            else
            {
                fadeScale = 1f;
            }

            Source.volume = _entryVolume * _clipVolume * _volumeScale *
                            Mathf.Clamp01(fadeScale) * groupVolume * masterVolume;
        }

        /// <summary>按剪辑权重随机选择一个剪辑；权重全为 0 时取第一个。</summary>
        public static AudioClipInfo PickRandomClip(AudioClipInfo[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            long totalWeight = 0;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] != null)
                {
                    totalWeight += clips[i].Weight;
                }
            }

            if (totalWeight <= 0)
            {
                return clips[0];
            }

            long roll = (long)UnityEngine.Random.Range(0f, (float)totalWeight);
            for (int i = 0; i < clips.Length; i++)
            {
                AudioClipInfo clip = clips[i];
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

            return clips[0];
        }

        /// <summary>迁移播放状态到目标音源并请求淡出（BGM 停止/切换时由 <see cref="AudioManager"/> 调用）。</summary>
        /// <param name="target">已从对象池取出的目标音源。</param>
        internal void DetachForFadeOut(AudioSource target)
        {
            target.clip = Source.clip;
            target.loop = Source.loop;
            target.pitch = Source.pitch;
            target.outputAudioMixerGroup = Source.outputAudioMixerGroup;
            target.volume = Source.volume;
            target.time = Source.time;
            target.Play();

            Source.Stop();
            Source.clip = null;

            Source = target;
            IsBgm = false;
            RequestStop();
        }

        /// <summary>按区间随机取值，自动处理上下限颠倒。</summary>
        private static float RandomRange(Vector2 range)
        {
            float min = Mathf.Min(range.x, range.y);
            float max = Mathf.Max(range.x, range.y);
            return UnityEngine.Random.Range(min, max);
        }
    }
}
