using System;
using System.Globalization;

namespace GameEngine
{
    /// <summary>将框架语言和货币枚举转换为对应的区域性信息。</summary>
    public static class CultureInfoUtility
    {
        /// <summary>获取指定界面语言的默认区域性信息。</summary>
        public static CultureInfo GetCultureInfo(Language language)
        {
            return CultureInfo.GetCultureInfo(GetCultureName(language));
        }

        /// <summary>获取指定结算货币的默认区域性信息。</summary>
        public static CultureInfo GetCultureInfo(Currency currency)
        {
            return CultureInfo.GetCultureInfo(GetCultureName(currency));
        }

        /// <summary>
        /// 获取使用指定语言格式和指定货币规则的区域性信息。
        /// 返回独立的可写副本，不会修改 .NET 缓存的共享区域性信息。
        /// </summary>
        public static CultureInfo GetCultureInfo(Language language, Currency currency)
        {
            CultureInfo culture = (CultureInfo)GetCultureInfo(language).Clone();
            NumberFormatInfo currencyFormat = GetCultureInfo(currency).NumberFormat;
            culture.NumberFormat.CurrencySymbol = currencyFormat.CurrencySymbol;
            culture.NumberFormat.CurrencyDecimalDigits = currencyFormat.CurrencyDecimalDigits;
            return culture;
        }

        private static string GetCultureName(Language language)
        {
            switch (language)
            {
                case Language.Chinese:
                    return "zh-CN";
                case Language.TraditionalChinese:
                    return "zh-TW";
                case Language.Korean:
                    return "ko-KR";
                case Language.Japanese:
                    return "ja-JP";
                case Language.English:
                    return "en-US";
                case Language.German:
                    return "de-DE";
                case Language.French:
                    return "fr-FR";
                case Language.Thai:
                    return "th-TH";
                case Language.Indonesian:
                    return "id-ID";
                case Language.Arabic:
                    return "ar-SA";
                case Language.Turkish:
                    return "tr-TR";
                case Language.Spanish:
                    return "es-ES";
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), language, "Unsupported language.");
            }
        }

        private static string GetCultureName(Currency currency)
        {
            switch (currency)
            {
                case Currency.CNY:
                    return "zh-CN";
                case Currency.USD:
                    return "en-US";
                case Currency.JPY:
                    return "ja-JP";
                case Currency.KRW:
                    return "ko-KR";
                case Currency.EUR:
                    return "de-DE";
                case Currency.RUB:
                    return "ru-RU";
                case Currency.VND:
                    return "vi-VN";
                case Currency.THB:
                    return "th-TH";
                case Currency.IDR:
                    return "id-ID";
                default:
                    throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unsupported currency.");
            }
        }
    }
}
