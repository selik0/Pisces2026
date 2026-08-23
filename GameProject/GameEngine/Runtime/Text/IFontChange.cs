using UnityEngine;

namespace GameEngine
{
    public interface IFontChange
    {
        int OriginalFontSize { get; }
        int OriginalLineSpacing { get;}
        string OriginalFontName { get; }
        void ChangeFont(Font font, FontLanguageEntry entry);
    }
}
