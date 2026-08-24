using System;
using System.Globalization;

namespace GameEngine
{
    /// <summary>
    /// 时间格式化工具，使用通过 <see cref="SetLanguage"/> 设置的语言区域格式输出日期、时间和时长文本。
    /// </summary>
    /// <remarks>
    /// <para>以下 DateTime 示例以简体中文区域和 2026-08-24 15:06:09 为例；标准格式的分隔符、顺序和本地化文本会随当前语言变化：</para>
    /// <para><c>d</c>：短日期，如“2026/8/24”；<c>D</c>：长日期，如“2026年8月24日”。</para>
    /// <para><c>t</c>：短时间，如“15:06”；<c>T</c>：长时间，如“15:06:09”。</para>
    /// <para><c>g</c>：短日期和短时间，如“2026/8/24 15:06”；<c>G</c>：短日期和长时间，如“2026/8/24 15:06:09”。</para>
    /// <para><c>f</c>：长日期和短时间；<c>F</c>：长日期和长时间。</para>
    /// <para><c>M</c> 或 <c>m</c>：月日，如“8月24日”；<c>Y</c> 或 <c>y</c>：年月，如“2026年8月”。</para>
    /// <para><c>O</c> 或 <c>o</c>：保留 DateTimeKind 的往返格式，如“2026-08-24T15:06:09.0000000”。</para>
    /// <para><c>R</c> 或 <c>r</c>：固定英文的 RFC1123 格式，如“Mon, 24 Aug 2026 15:06:09 GMT”；调用方应确保输入时间已转换为 UTC。</para>
    /// <para>常用自定义参数：<c>yyyy</c> 年、<c>MM</c> 月、<c>dd</c> 日、<c>HH</c> 24 小时、<c>hh</c> 12 小时、<c>mm</c> 分、<c>ss</c> 秒、<c>fff</c> 毫秒、<c>ddd</c> 星期缩写、<c>dddd</c> 星期全称、<c>tt</c> 上午/下午、<c>zzz</c> 时区偏移。</para>
    /// <para>例如 <c>yyyy-MM-dd HH:mm:ss</c> 显示“2026-08-24 15:06:09”，<c>MM/dd HH:mm</c> 显示“08/24 15:06”。</para>
    /// <para>以下 TimeSpan 示例以 1 天 2 小时 3 分 4 秒为例：</para>
    /// <para><c>d'.'hh':'mm':'ss</c>：显示“1.02:03:04”；<c>hh':'mm':'ss</c>：显示“02:03:04”；<c>mm':'ss</c>：显示“03:04”。</para>
    /// <para>TimeSpan 中 <c>d</c> 为天、<c>hh</c> 为不足一天的小时、<c>mm</c> 为不足一小时的分钟、<c>ss</c> 为不足一分钟的秒；冒号和点号等字面量必须使用单引号或反斜杠转义。</para>
    /// </remarks>
    public static class TimeFormatUtility
    {
        private static Language _language = Language.Chinese;
        private static CultureInfo _cultureInfo = CultureInfoUtility.GetCultureInfo(Language.Chinese);

        /// <summary>设置格式化语言，并更新后续所有格式化接口使用的区域性信息。</summary>
        /// <param name="language">框架支持的界面语言。</param>
        public static void SetLanguage(Language language)
        {
            _language = language;
            _cultureInfo = CultureInfoUtility.GetCultureInfo(language);
        }

        /// <summary>
        /// 按当前语言格式化日期时间。
        /// </summary>
        /// <param name="dateTime">需要格式化的日期时间。</param>
        /// <param name="format">标准或自定义 DateTime 格式；为 null 或空字符串时使用 <c>F</c>，即当前语言的短日期和长时间格式。</param>
        public static string Format(DateTime dateTime, string format = "F")
        {
            return dateTime.ToString(string.IsNullOrEmpty(format) ? "F" : format, _cultureInfo);
        }

        /// <summary>
        /// 将服务器时区 Unix 秒时间戳转换为服务器当地时间，并按当前语言格式化。
        /// </summary>
        /// <param name="time">服务器时区 Unix 秒时间戳，与 <see cref="TimeUtility.ServerNowSeconds"/> 使用相同语义。</param>
        /// <param name="format">标准或自定义 DateTime 格式；为 null 或空字符串时使用 <c>G</c>。</param>
        public static string FormatTimestamp(long time, string format = "F")
        {
            DateTime dateTime = TimeUtility.ToServerTime(time, false);
            return Format(dateTime, format);
        }

        /// <summary>
        /// 按当前语言格式化时长。
        /// </summary>
        /// <param name="timeSpan">需要格式化的时长。</param>
        /// <param name="format">标准或自定义 TimeSpan 格式；为 null 或空字符串时使用固定格式 <c>c</c>，例如“1.02:03:04”。</param>
        public static string Format(TimeSpan timeSpan, string format = "c")
        {
            return timeSpan.ToString(string.IsNullOrEmpty(format) ? "c" : format);
        }

        /// <summary>
        /// 将剩余秒数转换为非负 <see cref="TimeSpan"/> 并格式化。
        /// </summary>
        /// <param name="remainingSeconds">剩余秒数；小于 0 时按 0 处理。</param>
        /// <param name="format">标准或自定义 TimeSpan 格式；为 null 或空字符串时使用固定格式 <c>c</c>。例如传入 <c>hh':'mm':'ss</c>，3723 秒显示为“01:02:03”。</param>
        public static string FormatRemainingTime(float remainingSeconds, string format = "c")
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Max(0f, remainingSeconds));
            return Format(timeSpan, format);
        }
    }
}
