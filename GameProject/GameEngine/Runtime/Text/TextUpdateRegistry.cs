using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>注册需要随界面语言变化更新字体的文本对象。</summary>
    public static class TextUpdateRegistry
    {
        private static readonly HashSet<IFontChange> Texts = new HashSet<IFontChange>();
        private static readonly List<IFontChange> UpdateSnapshot = new List<IFontChange>();

        /// <summary>注册文本对象，并立即应用当前语言字体。重复注册无效。</summary>
        public static void Register(IFontChange text)
        {
            if (IsInvalid(text) || !Texts.Add(text))
            {
                return;
            }

            TextManager.Instance.ApplyFont(text);
        }

        /// <summary>注销文本对象。文本销毁或不再参与语言更新时必须调用。</summary>
        public static void Unregister(IFontChange text)
        {
            if (text != null)
            {
                Texts.Remove(text);
            }
        }

        /// <summary>按当前语言更新全部有效文本，并移除已销毁的 Unity 对象。</summary>
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
