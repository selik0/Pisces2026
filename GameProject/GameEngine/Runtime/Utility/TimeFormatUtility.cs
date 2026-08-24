using System;
using System.Globalization;

namespace GameEngine
{
    /// <summary>
    /// 时间格式化工具，使用当前界面语言对应的 <see cref="CultureInfo"/> 输出日期、时间和时长文本。
    /// </summary>
    /// <remarks>
    /// <para>以 2026-08-24 15:06:09 为例，常用 DateTime 格式参数如下：</para>
    /// <para><c>d</c>：短日期，如“2026/8/24”；<c>D</c>：长日期，如“2026年8月24日”。</para>
    /// <para><c>t</c>：短时间，如“15:06”；<c>T</c>：长时间，如“15:06:09”。</para>
    /// <para><c>g</c>：短日期和短时间，如“2026/8/24 15:06”；<c>G</c>：短日期和长时间，如“2026/8/24 15:06:09”。</para>
    /// <para><c>f</c>：长日期和短时间；<c>F</c>：长日期和长时间。</para>
    /// <para><c>M</c> 或 <c>m</c>：月日，如“8月24日”；<c>Y</c> 或 <c>y</c>：年月，如“2026年8月”。</para>
    /// <para><c>O</c> 或 <c>o</c>：往返格式，如“2026-08-24T15:06:09.0000000”。</para>
    /// <para><c>R</c> 或 <c>r</c>：RFC1123 格式，如“Mon, 24 Aug 2026 15:06:09 GMT”。</para>
    /// <para>常用自定义参数：<c>yyyy</c> 年、<c>MM</c> 月、<c>dd</c> 日、<c>HH</c> 24 小时、<c>hh</c> 12 小时、<c>mm</c> 分、<c>ss</c> 秒、<c>fff</c> 毫秒、<c>ddd</c> 星期缩写、<c>dddd</c> 星期全称、<c>tt</c> 上午/下午、<c>zzz</c> 时区偏移。</para>
    /// <para>例如 <c>yyyy-MM-dd HH:mm:ss</c> 显示“2026-08-24 15:06:09”，<c>MM/dd HH:mm</c> 显示“08/24 15:06”。</para>
    /// <para>以 1 天 2 小时 3 分 4 秒为例，常用 TimeSpan 自定义参数如下：</para>
    /// <para><c>d'.'hh':'mm':'ss</c>：显示“1.02:03:04”；<c>hh':'mm':'ss</c>：显示“02:03:04”；<c>mm':'ss</c>：显示“03:04”。</para>
    /// <para>TimeSpan 中 <c>d</c> 为天、<c>hh</c> 为不足一天的小时、<c>mm</c> 为不足一小时的分钟、<c>ss</c> 为不足一分钟的秒；冒号和点号等字面量必须使用单引号或反斜杠转义。</para>
    /// </remarks>
    public static class TimeFormatUtility
    {
        private static CultureInfo _cultureInfo = CultureInfoUtility.GetCultureInfo(Language.Chinese);

        /// <summary>设置格式化语言，并更新后续格式化使用的区域性信息。</summary>
        public static void SetLanguage(Language language)
        {
            _cultureInfo = CultureInfoUtility.GetCultureInfo(language);
        }

        /// <summary>
        /// 按当前语言格式化日期时间。
        /// </summary>
        /// <param name="dateTime">需要格式化的日期时间。</param>
        /// <param name="format">标准或自定义 DateTime 格式；为空时使用当前语言的常规日期和长时间格式。</param>
        public static string Format(DateTime dateTime, string format = "G")
        {
            return dateTime.ToString(string.IsNullOrEmpty(format) ? "G" : format, _cultureInfo);
        }

        /// <summary>
        /// 将 UTC Unix 秒时间戳转换为指定时区的当地时间并按当前语言格式化。
        /// </summary>
        /// <param name="utcTimestamp">UTC Unix 秒时间戳。</param>
        /// <param name="timezoneOffsetSeconds">目标时区相对 UTC 的偏移秒数，东区为正、西区为负。</param>
        /// <param name="format">标准或自定义 DateTime 格式；为空时使用 <c>G</c>。</param>
        public static string FormatTimestamp(long utcTimestamp, int timezoneOffsetSeconds = 0, string format = "G")
        {
            DateTime dateTime = DateTimeOffset.FromUnixTimeSeconds(utcTimestamp)
                .ToOffset(TimeSpan.FromSeconds(timezoneOffsetSeconds))
                .DateTime;
            return Format(dateTime, format);
        }

        /// <summary>
        /// 按当前语言格式化时长。
        /// </summary>
        /// <param name="timeSpan">需要格式化的时长。</param>
        /// <param name="format">标准或自定义 TimeSpan 格式；为空时使用 <c>c</c>，例如“1.02:03:04”。</param>
        public static string Format(TimeSpan timeSpan, string format = "c")
        {
            return timeSpan.ToString(string.IsNullOrEmpty(format) ? "c" : format, _cultureInfo);
        }
    }
}
