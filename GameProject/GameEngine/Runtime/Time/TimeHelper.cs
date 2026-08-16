using System;

namespace GameEngine
{
    /// <summary>
    /// 时间处理助手：维护以服务器时间为准的全局时钟，并提供常用时间换算与格式化方法。
    /// <para>
    /// 服务器下发其当前时间（Unix 秒）与时区时差（秒），本地通过记录同步时刻的
    /// <c>Time.realtimeSinceStartup</c> 推算当前服务器时间，避免依赖客户端本地时钟。
    /// </para>
    /// </summary>
    public static class TimeHelper
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private const long SecondsPerDay = 86400;
        private const long SecondsPerHour = 3600;
        private const long SecondsPerMinute = 60;

        private static long _serverTimeSeconds;    // 同步时刻的服务器时间（Unix 秒）
        private static float _syncRealtime;        // 同步时刻的 Time.realtimeSinceStartup
        private static int _timezoneOffsetSeconds; // 服务器时区相对 UTC 的时差（秒）

        /// <summary>服务器所在时区相对 UTC 的时差（秒，东区为正、西区为负）。</summary>
        public static int TimezoneOffsetSeconds => _timezoneOffsetSeconds;

        /// <summary>当前服务器 UTC 时间秒数（Unix 秒）。</summary>
        public static long ServerUtcNowSeconds => _serverTimeSeconds + (long)(UnityEngine.Time.realtimeSinceStartup - _syncRealtime);

        /// <summary>当前服务器时间秒数（服务器时区墙上时间）。</summary>
        public static long ServerNowSeconds => ServerUtcNowSeconds + _timezoneOffsetSeconds;

        /// <summary>当前服务器 UTC 时间。</summary>
        public static DateTime ServerUtcNow  => UnixEpoch.AddSeconds(ServerUtcNowSeconds);

        /// <summary>当前服务器墙上时间（服务器时区）。</summary>
        public static DateTime ServerNow => UnixEpoch.AddSeconds(ServerNowSeconds);

        /// <summary>
        /// 同步服务器当前时间与时区时差。
        /// </summary>
        /// <param name="serverTimeSeconds">服务器当前时间（Unix 秒，自 1970-01-01 UTC 起）。</param>
        /// <param name="timezoneOffsetSeconds">服务器时区相对 UTC 的时差（秒，东区为正、西区为负）。</param>
        public static void SyncServerTime(long serverTimeSeconds, int timezoneOffsetSeconds)
        {
            _serverTimeSeconds = serverTimeSeconds;
            _syncRealtime = UnityEngine.Time.realtimeSinceStartup;
            _timezoneOffsetSeconds = timezoneOffsetSeconds;

            Log.Debug($"[Time] SyncServerTime  serverTime={serverTimeSeconds}s  offset={timezoneOffsetSeconds}s");
        }
    }
}
