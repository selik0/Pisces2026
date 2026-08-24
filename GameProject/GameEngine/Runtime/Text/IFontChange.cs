using UnityEngine;

namespace GameEngine
{
    /// <summary>支持按语言配置替换字体的文本对象。</summary>
    public interface IFontChange
    {
        /// <summary>初始字号。</summary>
        int OriginalFontSize { get; }

        /// <summary>初始行间距。</summary>
        int OriginalLineSpacing { get; }

        /// <summary>初始字体名称，用于匹配字体配置。</summary>
        string OriginalFontName { get; }

        /// <summary>应用目标字体及其字号、行距配置。</summary>
        /// <param name="font">目标字体。</param>
        /// <param name="entry">字体语言配置。</param>
        void ChangeFont(Font font, FontLanguageEntry entry);
    }
}
