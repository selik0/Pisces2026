using System;

namespace GameEngine
{
    /// <summary>
    /// 时间处理助手：维护以服务器时间为准的全局时钟，并提供常用时间换算方法。
    /// <para>
    /// 服务器下发其当前时间（Unix 秒）与时区时差（秒），本地通过记录同步时刻的
    /// <c>Time.realtimeSinceStartup</c> 推算当前服务器时间，不依赖客户端本地时钟。
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
        private static long _serverOpenTimeSeconds;

        /// <summary>获取服务器时区相对 UTC 的时差（秒），东区为正、西区为负。</summary>
        public static int TimezoneOffsetSeconds => _timezoneOffsetSeconds;

        /// <summary>获取开服时间的服务器时区 Unix 秒时间戳，由 <see cref="SetServerOpenTime"/> 设置。</summary>
        /// <remarks>不随服务器时间同步更新。</remarks>
        public static long ServerOpenTimeSeconds => _serverOpenTimeSeconds;

        /// <summary>获取服务器时区下的开服时间。</summary>
        public static DateTime ServerOpenTime => ToServerTime(_serverOpenTimeSeconds, false);

        /// <summary>获取当前服务器时间的 UTC Unix 秒时间戳，不依赖客户端系统时钟。</summary>
        public static long ServerUtcNowSeconds => _serverTimeSeconds + (long)(UnityEngine.Time.realtimeSinceStartup - _syncRealtime);

        /// <summary>获取当前服务器时区的 Unix 秒时间戳（UTC 时间戳加时区偏移）。</summary>
        public static long ServerNowSeconds => ServerUtcNowSeconds + _timezoneOffsetSeconds;

        /// <summary>获取当前服务器 UTC 时间。</summary>
        public static DateTime ServerUtcNow  => UnixEpoch.AddSeconds(ServerUtcNowSeconds);

        /// <summary>获取当前服务器时区的当地时间。</summary>
        public static DateTime ServerNow => UnixEpoch.AddSeconds(ServerNowSeconds);

        /// <summary>设置服务器开服时间，内部统一保存为服务器时区时间戳。</summary>
        /// <param name="serverOpenTimeSeconds">开服时间，Unix 秒。</param>
        /// <param name="isUtc">时间戳是否为 UTC，默认为服务器时区。</param>
        public static void SetServerOpenTime(long serverOpenTimeSeconds, bool isUtc = false)
        {
            DateTime openTime = ToServerTime(serverOpenTimeSeconds, isUtc);
            _serverOpenTimeSeconds = ToTimestamp(openTime, false);
        }

        /// <summary>同步服务器当前时间和时区，作为后续时间推算的基准。</summary>
        /// <param name="serverTimeSeconds">服务器当前时间，UTC Unix 秒。</param>
        /// <param name="timezoneOffsetSeconds">时区相对 UTC 的时差（秒），东区为正、西区为负。</param>
        /// <remarks>同步后通过 Unity 实时运行时间推算当前服务器时间。</remarks>
        public static void SyncServerTime(long serverTimeSeconds, int timezoneOffsetSeconds)
        {
            _serverTimeSeconds = serverTimeSeconds;
            _syncRealtime = UnityEngine.Time.realtimeSinceStartup;
            _timezoneOffsetSeconds = timezoneOffsetSeconds;

            Log.Debug($"[Time] SyncServerTime  serverTime={serverTimeSeconds}s  offset={timezoneOffsetSeconds}s");
        }

        /// <summary>获取时间戳在服务器时区对应日期 00:00:00 的时间戳。</summary>
        /// <param name="timestamp">Unix 秒时间戳。</param>
        /// <param name="offsetSeconds">在当天零点基础上额外增加的秒数，默认为 0。</param>
        /// <param name="isUtc">时间戳是否为 UTC，默认为服务器时区。</param>
        /// <remarks>返回值与传入时间戳使用相同的时间戳类型。</remarks>
        public static long GetDayStartTimestamp(long timestamp, long offsetSeconds = 0, bool isUtc = false)
        {
            DateTime localTime = ToServerTime(timestamp, isUtc);
            DateTime localDayStart = localTime.Date;
            return ToTimestamp(localDayStart, isUtc) + offsetSeconds;
        }

        /// <summary>判断两个时间戳在服务器时区下是否属于同一年。</summary>
        /// <param name="firstTimestamp">第一个 Unix 秒时间戳。</param>
        /// <param name="secondTimestamp">第二个 Unix 秒时间戳。</param>
        /// <param name="isUtc">时间戳是否为 UTC，默认为服务器时区。</param>
        public static bool IsSameYear(long firstTimestamp, long secondTimestamp, bool isUtc = false)
        {
            return ToServerTime(firstTimestamp, isUtc).Year == ToServerTime(secondTimestamp, isUtc).Year;
        }

        /// <summary>判断两个时间戳在服务器时区下是否属于同一月。</summary>
        /// <param name="firstTimestamp">第一个 Unix 秒时间戳。</param>
        /// <param name="secondTimestamp">第二个 Unix 秒时间戳。</param>
        /// <param name="isUtc">时间戳是否为 UTC，默认为服务器时区。</param>
        public static bool IsSameMonth(long firstTimestamp, long secondTimestamp, bool isUtc = false)
        {
            DateTime firstTime = ToServerTime(firstTimestamp, isUtc);
            DateTime secondTime = ToServerTime(secondTimestamp, isUtc);
            return firstTime.Year == secondTime.Year && firstTime.Month == secondTime.Month;
        }

        /// <summary>判断两个时间戳在服务器时区下是否属于同一周，每周从周一开始。</summary>
        /// <param name="firstTimestamp">第一个 Unix 秒时间戳。</param>
        /// <param name="secondTimestamp">第二个 Unix 秒时间戳。</param>
        /// <param name="isUtc">时间戳是否为 UTC，默认为服务器时区。</param>
        public static bool IsSameWeek(long firstTimestamp, long secondTimestamp, bool isUtc = false)
        {
            return GetWeekStartTimestamp(firstTimestamp, isUtc) == GetWeekStartTimestamp(secondTimestamp, isUtc);
        }

        /// <summary>判断两个时间戳在服务器时区下是否属于同一天。</summary>
        /// <param name="firstTimestamp">第一个 Unix 秒时间戳。</param>
        /// <param name="secondTimestamp">第二个 Unix 秒时间戳。</param>
        /// <param name="isUtc">时间戳是否为 UTC，默认为服务器时区。</param>
        public static bool IsSameDay(long firstTimestamp, long secondTimestamp, bool isUtc = false)
        {
            return GetDayStartTimestamp(firstTimestamp, isUtc: isUtc) == GetDayStartTimestamp(secondTimestamp, isUtc: isUtc);
        }

        /// <summary>获取时间戳所在周周一 00:00:00 起偏移指定秒数后的时间戳。</summary>
        /// <param name="timestamp">Unix 秒时间戳。</param>
        /// <param name="secondTimestamp">从周一零点起计算的秒数偏移，默认为 0。</param>
        /// <param name="isUtc">时间戳是否为 UTC，默认为服务器时区。</param>
        public static long GetWeekTimestamp(long timestamp, long secondTimestamp = 0, bool isUtc = false)
        {
            DateTime localTime = ToServerTime(timestamp, isUtc);
            int daysFromMonday = ((int)localTime.DayOfWeek + 6) % 7;
            DateTime monday = localTime.Date.AddDays(-daysFromMonday);
            DateTime targetTime = monday.AddSeconds(secondTimestamp);
            return ToTimestamp(targetTime, isUtc);
        }

        /// <summary>获取两个时间戳相差的自然日数量，始终为非负数。</summary>
        /// <param name="firstTimestamp">第一个 Unix 秒时间戳。</param>
        /// <param name="secondTimestamp">第二个 Unix 秒时间戳。</param>
        /// <param name="offsetSeconds">计算日期前统一增加的秒数，用于调整自然日切换时刻。</param>
        /// <param name="isUtc">时间戳是否为 UTC，默认为服务器时区。</param>
        public static int GetCrossDayCount(long firstTimestamp, long secondTimestamp, long offsetSeconds = 0, bool isUtc = false)
        {
            long firstDay = GetDayStartTimestamp(firstTimestamp, offsetSeconds, isUtc);
            long secondDay = GetDayStartTimestamp(secondTimestamp, offsetSeconds, isUtc);
            return checked((int)Math.Abs((secondDay - firstDay) / SecondsPerDay));
        }

        /// <summary>获取时间戳所在周周一 00:00:00 的 Unix 秒时间戳。</summary>
        private static long GetWeekStartTimestamp(long timestamp, bool isUtc)
        {
            DateTime localTime = ToServerTime(timestamp, isUtc);
            int daysFromMonday = ((int)localTime.DayOfWeek + 6) % 7;
            return ToTimestamp(localTime.Date.AddDays(-daysFromMonday), isUtc);
        }

        /// <summary>将 Unix 秒时间戳转换为服务器时区当地时间。</summary>
        private static DateTime ToServerTime(long timestamp, bool isUtc)
        {
            return UnixEpoch.AddSeconds(timestamp + (isUtc ? _timezoneOffsetSeconds : 0));
        }

        /// <summary>将服务器时区当地时间转换为 Unix 秒时间戳。</summary>
        private static long ToTimestamp(DateTime serverTime, bool isUtc)
        {
            return checked((long)(serverTime - UnixEpoch).TotalSeconds) - (isUtc ? _timezoneOffsetSeconds : 0);
        }
    }
}
