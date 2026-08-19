using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>
    /// 音频表封装。
    /// <para>
    /// 维护音频 id 到 <see cref="AudioEntry"/> 的映射，提供按 id 获取音频配置的能力。
    /// 实例由调用方创建并持有，音频资产通过 <see cref="AddAudios"/> 批量注册，
    /// 本类不负责解析音频表文件与实际播放，也不提供移除接口。
    /// </para>
    ///
    /// <para><b>约定</b></para>
    /// <list type="bullet">
    ///   <item>id 重复注册时记录警告并以新条目覆盖</item>
    ///   <item>非法条目（资产已销毁或剪辑列表为空）记录错误并跳过</item>
    ///   <item><see cref="AudioEntry"/> 为 Unity 对象，判空遵循 Unity 假 null 语义</item>
    /// </list>
    /// </summary>
    public sealed class AudioTable
    {
        // ── 状态 ─────────────────────────────────────────────────────────────────

        private readonly Dictionary<uint, AudioEntry> _audios = new Dictionary<uint, AudioEntry>();

        /// <summary>当前已注册的音频数量。</summary>
        public int Count => _audios.Count;

        // ── 注册 ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// 批量注册音频，通常由配置加载流程调用。
        /// <para>id 重复时记录警告并以新条目覆盖，非法条目记录错误并跳过。</para>
        /// </summary>
        /// <param name="entries">音频条目集合，不可为 null</param>
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

        /// <summary>注册单条音频，供 <see cref="AddAudios"/> 内部复用。</summary>
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

        /// <summary>
        /// 获取指定 id 的音频配置条目。
        /// </summary>
        /// <param name="id">音频 id</param>
        /// <returns>音频条目；不存在或资产已销毁时记录警告并返回 null</returns>
        public AudioEntry GetAudio(uint id)
        {
            if (_audios.TryGetValue(id, out AudioEntry entry))
            {
                // 资产可能在注册后被外部销毁，按 Unity 假 null 语义再校验一次
                if (entry == null)
                {
                    Log.Error($"[Audio] GetAudio failed: id={id} 条目资产已销毁");
                    return null;
                }

                return entry;
            }

            Log.Warning($"[Audio] GetAudio failed: id={id} not found");
            return null;
        }
    }
}
