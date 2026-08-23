using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>音频数据抽象基类，维护音频 id 到 <see cref="AudioEntry"/> 的映射。</summary>
    public abstract class AudioLanguageData : LocalizationData<AudioLanguage>
    {
        private readonly Dictionary<uint, AudioEntry> _audios = new Dictionary<uint, AudioEntry>();

        /// <summary>已注册音频数量。</summary>
        public int Count => _audios.Count;

        /// <summary>批量注册音频，重复 id 覆盖，非法条目跳过。</summary>
        /// <param name="entries">音频条目集合</param>
        public void AddAudios(IEnumerable<AudioEntry> entries)
        {
            if (entries == null)
            {
                Log.Error("[Audio] AddAudios failed: entries is null");
                return;
            }

            int total = 0;
            int success = 0;

            foreach (AudioEntry entry in entries)
            {
                total++;

                if (AddAudio(entry))
                {
                    success++;
                }
            }

            Log.Debug($"[Audio] AddAudios 完成: total={total} success={success}");
        }

        /// <summary>注册单条音频。</summary>
        private bool AddAudio(AudioEntry entry)
        {
            if (entry == null)
            {
                Log.Error("[Audio] AddAudio failed: entry is null");
                return false;
            }

            if (!entry.HasClips())
            {
                Log.Error($"[Audio] AddAudio failed: id={entry.Id} name={entry.name} 剪辑列表为空");
                return false;
            }

            if (_audios.TryGetValue(entry.Id, out AudioEntry old))
            {
                Log.Warning($"[Audio] id={entry.Id} name={entry.name} 已存在，旧条目被覆盖");
            }

            _audios[entry.Id] = entry;
            return true;
        }

        // ── 查询 ─────────────────────────────────────────────────────────────────

        /// <summary>获取指定 id 的音频配置，不存在时返回 null。</summary>
        /// <param name="id">音频 id</param>
        public AudioEntry GetEntry(uint id)
        {
            if (_audios.TryGetValue(id, out AudioEntry entry))
            {
                // 按 Unity 假 null 语义再校验一次
                if (entry == null)
                {
                    Log.Error($"[Audio] GetEntry failed: id={id} 条目资产已销毁");
                    return null;
                }

                return entry;
            }

            Log.Warning($"[Audio] GetEntry failed: id={id} not found");
            return null;
        }
    }
}
