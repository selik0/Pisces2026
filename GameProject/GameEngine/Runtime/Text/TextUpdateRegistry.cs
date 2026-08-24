using System;
using System.Collections.Generic;

namespace GameEngine
{
    /// <summary>注册需要随界面语言变化更新字体的文本对象。</summary>
    public static class TextUpdateRegistry
    {
        private static readonly HashSet<IFontChange> Texts = new HashSet<IFontChange>();
        private static readonly List<IFontChange> UpdateSnapshot = new List<IFontChange>();

        /// <summary>当前注册的文本数量，包含尚未清理的已销毁对象。</summary>
        public static int Count => Texts.Count;

        /// <summary>注册文本对象，并立即应用当前语言字体。重复注册无效。</summary>
        public static void Register(IFontChange text)
        {
            if (IsInvalid(text) || !Texts.Add(text))
            {
                return;
            }

            TextManager.Instance.ApplyFont(text);
        }

        /// <summary>注销不再参与语言更新的文本对象。</summary>
        public static void Unregister(IFontChange text)
        {
            if (text != null)
            {
                Texts.Remove(text);
            }
        }

        /// <summary>更新全部有效文本，并清理已销毁对象。</summary>
        public static void UpdateAll()
        {
            UpdateSnapshot.Clear();
            UpdateSnapshot.AddRange(Texts);

            for (int i = 0; i < UpdateSnapshot.Count; i++)
            {
                IFontChange text = UpdateSnapshot[i];
                if (IsInvalid(text))
                {
                    Texts.Remove(text);
                    continue;
                }

                try
                {
                    TextManager.Instance.ApplyFont(text);
                }
                catch (Exception exception)
                {
                    Log.Error("[TextUpdateRegistry] 更新字体异常。", exception);
                }
            }

            UpdateSnapshot.Clear();
        }

        /// <summary>清除全部注册对象。</summary>
        public static void Clear()
        {
            Texts.Clear();
            UpdateSnapshot.Clear();
        }

        private static bool IsInvalid(IFontChange text)
        {
            if (text == null)
            {
                return true;
            }

            return text is UnityEngine.Object unityObject && unityObject == null;
        }
    }
}
