using System;
using System.Globalization;

namespace GameEngine
{
    /// <summary>使用当前界面语言格式化日期、时间戳和剩余时间。</summary>
    /// <remarks>
    /// <para>以下 DateTime 标准格式示例使用简体中文区域和时间 2026-08-24 15:06:09；实际文本和分隔符会随 <see cref="TextManager.CultureInfo"/> 变化。</para>
    /// <list type="table">
    ///   <listheader><term>格式</term><description>含义与示例</description></listheader>
    ///   <item><term><c>d</c></term><description>短日期：“2026/8/24”</description></item>
    ///   <item><term><c>D</c></term><description>长日期：“2026年8月24日”</description></item>
    ///   <item><term><c>t</c></term><description>短时间：“15:06”</description></item>
    ///   <item><term><c>T</c></term><description>长时间：“15:06:09”</description></item>
    ///   <item><term><c>g</c></term><description>短日期和短时间：“2026/8/24 15:06”</description></item>
    ///   <item><term><c>G</c></term><description>短日期和长时间：“2026/8/24 15:06:09”</description></item>
    ///   <item><term><c>f</c></term><description>长日期和短时间：“2026年8月24日 15:06”</description></item>
    ///   <item><term><c>F</c></term><description>长日期和长时间：“2026年8月24日 15:06:09”</description></item>
    ///   <item><term><c>M</c> / <c>m</c></term><description>月日：“8月24日”</description></item>
    ///   <item><term><c>Y</c> / <c>y</c></term><description>年月：“2026年8月”</description></item>
    ///   <item><term><c>O</c> / <c>o</c></term><description>往返格式：“2026-08-24T15:06:09.0000000”</description></item>
    ///   <item><term><c>R</c> / <c>r</c></term><description>RFC1123：“Mon, 24 Aug 2026 15:06:09 GMT”；应传入 UTC 时间</description></item>
    ///   <item><term><c>s</c></term><description>可排序格式：“2026-08-24T15:06:09”</description></item>
    ///   <item><term><c>u</c></term><description>通用可排序 UTC 格式：“2026-08-24 15:06:09Z”</description></item>
    /// </list>
    /// </remarks>
    public static class TimeFormatUtility
    {
        /// <summary>按当前语言格式化日期时间。</summary>
        /// <param name="dateTime">需要格式化的日期时间。</param>
        /// <param name="format">DateTime 格式；为空时使用 <c>F</c>（长日期和长时间）。</param>
        public static string Format(DateTime dateTime, string format = "F")
        {
            return dateTime.ToString(string.IsNullOrEmpty(format) ? "F" : format, TextManager.CultureInfo);
        }

        /// <summary>将服务器时区时间戳转换为当地时间并格式化。</summary>
        /// <param name="time">服务器时区 Unix 秒时间戳，与 <see cref="TimeUtility.ServerNowSeconds"/> 使用相同语义。</param>
        /// <param name="format">DateTime 格式；为空时使用 <c>F</c>。</param>
        public static string FormatTimestamp(long time, string format = "F")
        {
            DateTime dateTime = TimeUtility.ToServerTime(time, false);
            return Format(dateTime, format);
        }

        /// <summary>格式化时长。</summary>
        /// <param name="timeSpan">需要格式化的时长。</param>
        /// <param name="format">TimeSpan 格式；为空时使用 <c>c</c>，例如“1.02:03:04”。</param>
        public static string Format(TimeSpan timeSpan, string format = "c")
        {
            return timeSpan.ToString(string.IsNullOrEmpty(format) ? "c" : format);
        }

        /// <summary>格式化非负剩余秒数。</summary>
        /// <param name="remainingSeconds">剩余秒数；小于 0 时按 0 处理。</param>
        /// <param name="format">TimeSpan 格式；例如 <c>hh':'mm':'ss</c> 将 3723 秒显示为“01:02:03”。</param>
        public static string FormatRemainingTime(float remainingSeconds, string format = "c")
        {
            TimeSpan timeSpan = TimeSpan.FromSeconds(Math.Max(0f, remainingSeconds));
            return Format(timeSpan, format);
        }
    }
}
