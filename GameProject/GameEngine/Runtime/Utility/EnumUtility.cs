using System;

namespace GameEngine
{
    /// <summary>
    /// 枚举解析助手。
    /// </summary>
    public static class EnumUtility
    {
        /// <summary>解析字符串为枚举，空值返回 false，解析失败记录警告。</summary>
        /// <typeparam name="TEnum">目标枚举类型。</typeparam>
        /// <param name="value">枚举名称字符串，忽略大小写。</param>
        /// <param name="result">解析结果，失败时为 default(TEnum)。</param>
        public static bool TryParseEnum<TEnum>(string value, out TEnum result) where TEnum : struct
        {
            if (string.IsNullOrEmpty(value))
            {
                Log.Error($"EnumUtility.TryParseEnum: 枚举名称为空，无法解析为 {typeof(TEnum).Name}");
                result = default(TEnum);
                return false;
            }

            if (!Enum.TryParse(value, true, out result))
            {
                Log.Error($"EnumUtility.TryParseEnum: 无法解析 \"{value}\" 为 {typeof(TEnum).Name}");
                result = default(TEnum);
                return false;
            }

            return true;
        }
    }
}
