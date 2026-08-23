using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace GameEngine
{
    /// <summary>
    /// 音频管理器单例，按 <see cref="AudioEntry"/> 配置播放 BGM 与音效。
    /// 音频配置由内部 <see cref="AudioLanguageTable"/> 保存（未初始化前为空），通过 <see cref="InitializeData"/> 注入。
    /// 基于 <see cref="MonoSingleton{T}"/> 自动创建常驻实例，自身 Update 驱动，不依赖主循环 Tick。
    /// 支持 <see cref="AudioPlayMode"/> 各播放模式、随机音量/音调与剪辑权重、淡入淡出、同层抢占、
    /// 分组混音与 BGM 独立通道；每个播放中的音频对应一个 <see cref="SoundInstance"/>。
    /// 剪辑默认从 Resources 加载，可通过 <see cref="SetClipLoader"/> 替换资源后端。
    /// </summary>
    public sealed class AudioManager : MonoSingleton<AudioManager>
    {
        /// <summary>音源对象池上限，超过后直接销毁多余音源对象。</summary>
        private const int MaxSourcePoolSize = 64;

        // ── 状态 ───────────────────────────────────────────────────────────────

        private readonly Dictionary<AudioMixType, AudioMixerGroup> _mixerGroups = new Dictionary<AudioMixType, AudioMixerGroup>();
        private readonly Dictionary<AudioMixType, float> _groupVolumes = new Dictionary<AudioMixType, float>();
        private readonly Dictionary<string, AudioClip> _clipCache = new Dictionary<string, AudioClip>();
        private readonly List<AudioSource> _sourcePool = new List<AudioSource>();
        private readonly List<SoundInstance> _instances = new List<SoundInstance>();

        private AudioSource _bgmSource;
        private SoundInstance _bgmInstance;
        private Func<string, AudioClip> _clipLoader;
        private AudioLanguageTable _table;

        /// <summary>全局音量缩放，作用于所有播放实例。</summary>
        public float MasterVolume { get; set; } = 1f;

        /// <summary>全局静音。静音时仍正常播放，仅输出音量归零。</summary>
        public bool Muted { get; set; }

        /// <summary>是否暂停全部音频（映射到 <see cref="AudioListener.pause"/>，需要场景存在 AudioListener）。</summary>
        public bool Paused
        {
            get { return AudioListener.pause; }
            set { AudioListener.pause = value; }
        }

        /// <summary>当前活跃的播放实例数量（含 BGM）。</summary>
        public int PlayingCount => _instances.Count;

        /// <summary>当前音频语言。</summary>
        public AudioLanguage Language { get; private set; }

        /// <summary>设置当前音频语言。</summary>
        /// <param name="language">音频语言</param>
        public void SetLanguage(AudioLanguage language)
        {
            Language = language;
            _table?.SetLanguage(language);
        }

        /// <summary>初始化音频表：将具体音频表赋给 Manager 并加载其数据。</summary>
        /// <param name="table">音频表，不可为 null</param>
        public void InitializeData(AudioLanguageTable table)
        {
            if (table == null)
            {
                Log.Error("[Audio] InitializeData failed: table is null");
                return;
            }
            _table = table;
            _table.SetLanguage(Language);
        }

        /// <summary>获取指定 id 的音频配置，未注册时记录警告并返回 null。</summary>
        public AudioEntry GetEntry(uint id)
        {
            return _table?.GetEntry(id);
        }

        // ── 生命周期 ───────────────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this)
            {
                return;
            }

            EnsureBgmSource();
            EnsureAudioListener();
            Log.Debug("[AudioManager] 初始化完成。");
        }

        private void Update()
        {
            if (_instances.Count == 0)
            {
                return;
            }

            UpdateInstances();
        }

        protected override void OnDestroy()
        {
            _instances.Clear();
            _sourcePool.Clear();
            _bgmInstance = null;
            _bgmSource = null;
            base.OnDestroy();
        }

        // ── 混音 ───────────────────────────────────────────────────────────────

        /// <summary>为混音分组注入 AudioMixerGroup，未注入的分组不设置混音输出。</summary>
        public void SetMixerGroup(AudioMixType type, AudioMixerGroup group)
        {
            if (group == null)
            {
                _mixerGroups.Remove(type);
                return;
            }

            _mixerGroups[type] = group;
        }

        /// <summary>设置分组音量（0 到 1），作用于该组所有播放实例。</summary>
        public void SetGroupVolume(AudioMixType type, float volume)
        {
            _groupVolumes[type] = Mathf.Clamp01(volume);
        }

        /// <summary>获取分组音量，未设置时返回 1。</summary>
        public float GetGroupVolume(AudioMixType type)
        {
            if (_groupVolumes.TryGetValue(type, out float volume))
            {
                return volume;
            }

            return 1f;
        }

        // ── 播放 ───────────────────────────────────────────────────────────────

        /// <summary>按 id 播放音频，BGM 分组走 BGM 通道，其余作为音效。</summary>
        /// <param name="id">音频条目 Id。</param>
        /// <param name="volumeScale">额外的音量缩放（默认 1）。</param>
        /// <returns>是否成功开始播放。</returns>
        public bool Play(uint id, float volumeScale = 1f)
        {
            AudioEntry entry = GetEntry(id);
            if (entry == null)
            {
                return false;
            }

            return PlayInternal(entry, Mathf.Max(0f, volumeScale), entry.MixerGroup == AudioMixType.Bgm);
        }

        /// <summary>播放 BGM，替换当前 BGM（旧 BGM 按自身配置淡出）。</summary>
        public bool PlayBgm(uint id)
        {
            AudioEntry entry = GetEntry(id);
            if (entry == null)
            {
                return false;
            }

            return PlayInternal(entry, 1f, true);
        }

        /// <summary>停止 BGM，按 <see cref="AudioEntry.DiscardTime"/> 淡出。</summary>
        public void StopBgm()
        {
            StopBgmInternal();
        }

        /// <summary>停止指定 id 的全部播放实例（BGM 与音效）。</summary>
        public void Stop(uint id)
        {
            if (_bgmInstance != null && _bgmInstance.EntryId == id)
            {
                StopBgmInternal();
            }

            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                SoundInstance instance = _instances[i];
                if (!instance.IsBgm && instance.EntryId == id)
                {
                    instance.RequestStop();
                }
            }
        }

        /// <summary>停止全部音效（保留 BGM）。</summary>
        public void StopAllSfx()
        {
            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                if (!_instances[i].IsBgm)
                {
                    _instances[i].RequestStop();
                }
            }
        }

        /// <summary>停止全部音频（BGM 与音效）。</summary>
        public void StopAll()
        {
            StopBgmInternal();
            StopAllSfx();
        }

        // ── 资源 ───────────────────────────────────────────────────────────────

        /// <summary>设置剪辑加载器，替换默认的 Resources 加载（可对接 Addressables 等）。</summary>
        public void SetClipLoader(Func<string, AudioClip> loader)
        {
            _clipLoader = loader;
            // 换加载器后旧缓存可能来自不同资源后端，一并清空防止串用
            _clipCache.Clear();
            Log.Debug("[AudioManager] 已更换剪辑加载器并清空剪辑缓存。");
        }

        /// <summary>清空剪辑缓存，释放已加载剪辑的引用。</summary>
        public void ClearCache()
        {
            _clipCache.Clear();
            Log.Debug("[AudioManager] 已清空音频剪辑缓存。");
        }

        // ── 内部：播放 ─────────────────────────────────────────────────────────

        /// <summary>统一的播放入口：BGM 走常驻音源通道，音效走对象池音源。</summary>
        private bool PlayInternal(AudioEntry entry, float volumeScale, bool isBgm)
        {
            if (!ValidateEntry(entry))
            {
                return false;
            }

            if (isBgm)
            {
                StopBgmInternal();
                EnsureBgmSource();
            }
            else
            {
                EnforceHierarchyLimit(entry);
            }

            AudioSource source = isBgm ? _bgmSource : AcquireSource();
            SoundInstance instance = CreateInstance(entry, source, volumeScale, isBgm);
            if (instance == null)
            {
                if (!isBgm)
                {
                    ReleaseSource(source);
                }

                return false;
            }

            if (isBgm)
            {
                _bgmInstance = instance;
            }

            string clipName = instance.CurrentClip != null ? instance.CurrentClip.name : "null";
            Log.Debug(isBgm
                ? $"[AudioManager] 播放 BGM id={entry.Id} name={entry.name} mode={entry.PlayMode}"
                : $"[AudioManager] 播放音效 id={entry.Id} name={entry.name} clip={clipName}");
            return true;
        }

        private bool ValidateEntry(AudioEntry entry)
        {
            if (entry == null)
            {
                Log.Error("[AudioManager] 播放失败：entry 为 null。");
                return false;
            }

            if (!entry.CanPlay())
            {
                Log.Error($"[AudioManager] 播放失败：id={entry.Id} name={entry.name} 未配置可用剪辑。");
                return false;
            }

            return true;
        }

        /// <summary>停止当前 BGM：按自身 <see cref="AudioEntry.DiscardTime"/> 迁移到临时音源淡出，或立即停止。</summary>
        private void StopBgmInternal()
        {
            if (_bgmInstance == null || _bgmSource == null)
            {
                return;
            }

            SoundInstance old = _bgmInstance;
            _bgmInstance = null;

            if (old.State == SoundPlaybackState.FadingOut || !old.Source.isPlaying || old.DiscardTime <= 0f)
            {
                Recycle(old);
                return;
            }

            // 迁移到对象池音源继续淡出，BGM 常驻音源可立即被新 BGM 占用
            old.DetachForFadeOut(AcquireSource());
        }

        /// <summary>同层抢占：并发实例达到 <see cref="AudioEntry.MaxPlayNumOnce"/> 时，直接回收最早开始的实例。</summary>
        private void EnforceHierarchyLimit(AudioEntry entry)
        {
            if (entry.MaxPlayNumOnce <= 0)
            {
                return;
            }

            int count = 0;
            SoundInstance oldest = null;
            for (int i = 0; i < _instances.Count; i++)
            {
                SoundInstance instance = _instances[i];
                if (instance.IsBgm || instance.State == SoundPlaybackState.FadingOut)
                {
                    continue;
                }

                if (instance.Hierarchy != entry.SoundHierarchy)
                {
                    continue;
                }

                count++;
                if (oldest == null || instance.StartRealTime < oldest.StartRealTime)
                {
                    oldest = instance;
                }
            }

            if (count >= entry.MaxPlayNumOnce && oldest != null)
            {
                Recycle(oldest);
                Log.Debug($"[AudioManager] 同层抢占 id={entry.Id} hierarchy={entry.SoundHierarchy}，回收最旧实例。");
            }
        }

        // ── 内部：实例管理 ─────────────────────────────────────────────────────

        private SoundInstance CreateInstance(AudioEntry entry, AudioSource source, float volumeScale, bool isBgm)
        {
            bool isRandomLoop = entry.PlayMode == AudioPlayMode.RandomLoop;
            SoundInstance instance = new SoundInstance(entry, source, entry.IsLoop, isRandomLoop, volumeScale, isBgm);
            AudioClipInfo clipInfo = instance.InitialClip;
            if (clipInfo == null)
            {
                Log.Error($"[AudioManager] 播放失败：id={entry.Id} 无可播放剪辑。");
                return null;
            }

            AudioClip clip = LoadClip(clipInfo.Path);
            if (clip == null)
            {
                return null;
            }

            source.outputAudioMixerGroup = GetMixerGroup(entry.MixerGroup);

            // 剪辑循环由播放状态机推进，不启用音源单曲循环
            if (!instance.Play(clip, clipInfo.Volume, false))
            {
                return null;
            }

            _instances.Add(instance);
            instance.ApplyVolume(GetGroupVolume(entry.MixerGroup), Muted ? 0f : MasterVolume);
            return instance;
        }

        /// <summary>回收实例：BGM 复位常驻音源，音效归还对象池。</summary>
        private void Recycle(SoundInstance instance)
        {
            _instances.Remove(instance);

            if (instance.IsBgm)
            {
                if (_bgmInstance == instance)
                {
                    _bgmInstance = null;
                }

                if (_bgmSource != null)
                {
                    _bgmSource.Stop();
                    _bgmSource.clip = null;
                }

                return;
            }

            ReleaseSource(instance.Source);
        }

        // ── 内部：每帧驱动 ─────────────────────────────────────────────────────

        private void UpdateInstances()
        {
            float deltaTime = Time.unscaledDeltaTime;
            float masterVolume = Muted ? 0f : MasterVolume;

            for (int i = _instances.Count - 1; i >= 0; i--)
            {
                SoundInstance instance = _instances[i];
                if (!instance.Update(deltaTime))
                {
                    Recycle(instance);
                    continue;
                }

                // 当前剪辑播完（含停顿结束）：由管理器加载下一个剪辑并换曲
                if (instance.NeedsNextClip)
                {
                    AudioClipInfo clipInfo = instance.MoveToNextClip();
                    if (clipInfo == null)
                    {
                        Recycle(instance);
                        continue;
                    }

                    AudioClip clip = LoadClip(clipInfo.Path);
                    if (clip == null || !instance.Play(clip, clipInfo.Volume, false))
                    {
                        Recycle(instance);
                        continue;
                    }
                }

                instance.ApplyVolume(GetGroupVolume(instance.Group), masterVolume);
            }
        }

        // ── 内部：剪辑与音源 ───────────────────────────────────────────────────

        private AudioClip LoadClip(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                Log.Error("[AudioManager] 加载剪辑失败：path 为空。");
                return null;
            }

            if (_clipCache.TryGetValue(path, out AudioClip cached) && cached != null)
            {
                return cached;
            }

            AudioClip clip;
            try
            {
                clip = _clipLoader != null ? _clipLoader(path) : Resources.Load<AudioClip>(path);
            }
            catch (Exception exception)
            {
                Log.Error($"[AudioManager] 加载剪辑异常：{path}", exception);
                return null;
            }

            if (clip == null)
            {
                Log.Error($"[AudioManager] 未找到音频剪辑：{path}");
                return null;
            }

            _clipCache[path] = clip;
            return clip;
        }

        private AudioMixerGroup GetMixerGroup(AudioMixType type)
        {
            _mixerGroups.TryGetValue(type, out AudioMixerGroup group);
            return group;
        }

        private void EnsureBgmSource()
        {
            if (_bgmSource != null)
            {
                return;
            }

            GameObject bgmObject = new GameObject("_AudioBgm");
            bgmObject.transform.SetParent(transform, false);
            _bgmSource = bgmObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = false;
        }

        private void EnsureAudioListener()
        {
            if (FindObjectOfType<AudioListener>() == null)
            {
                gameObject.AddComponent<AudioListener>();
            }
        }

        /// <summary>从对象池取音源，池为空时创建新的音源对象。</summary>
        private AudioSource AcquireSource()
        {
            for (int i = _sourcePool.Count - 1; i >= 0; i--)
            {
                AudioSource source = _sourcePool[i];
                if (source != null)
                {
                    _sourcePool.RemoveAt(i);
                    source.gameObject.SetActive(true);
                    return source;
                }
            }

            GameObject go = new GameObject("Sfx");
            go.transform.SetParent(transform, false);
            AudioSource newSource = go.AddComponent<AudioSource>();
            newSource.playOnAwake = false;
            return newSource;
        }

        /// <summary>归还音源到对象池，池满时销毁音源对象。</summary>
        private void ReleaseSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.Stop();
            source.clip = null;
            source.outputAudioMixerGroup = null;

            if (_sourcePool.Count < MaxSourcePoolSize)
            {
                _sourcePool.Add(source);
                source.gameObject.SetActive(false);
            }
            else
            {
                Destroy(source.gameObject);
            }
        }
    }
}
