using GameNative;

namespace GameClient
{
    /// <summary>
    /// 基于 Unity 宏定义的平台判断实现。
    ///
    /// <para>
    /// 其中 UNITY_EDITOR / UNITY_STANDALONE / UNITY_ANDROID / UNITY_IOS / UNITY_WEBGL
    /// 为 Unity 官方内置宏；鸿蒙为团结引擎宏 UNITY_OPENHARMONY。
    /// 小游戏/快游戏平台无引擎内置宏，判断条件为「自定义宏 || 官方子平台宏」，
    /// 自定义宏（WXWEBGL/TTWEBGL/MTWEBGL/ZFBWEBGL/HMWEBGL/QUICKAPP）需在对应平台构建或转换时
    /// 通过 Scripting Define Symbols 定义；团结引擎官方子平台宏为
    /// MINIGAME_SUBPLATFORM_WEIXIN / MINIGAME_SUBPLATFORM_DOUYIN 等，激活对应子平台时自动定义。
    /// </para>
    /// </summary>
    public sealed class PlatformImp : IPlatform
    {
        /// <inheritdoc />
        public bool IsPC
        {
            get
            {
#if !UNITY_EDITOR && UNITY_STANDALONE
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsEditor
        {
            get
            {
#if UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsAndroidEditor
        {
            get
            {
#if UNITY_EDITOR && UNITY_ANDROID
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsIOSEditor
        {
            get
            {
#if UNITY_EDITOR && UNITY_IOS
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsPCOrEditor
        {
            get
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsAndroid
        {
            get
            {
#if !UNITY_EDITOR && UNITY_ANDROID
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsIOS
        {
            get
            {
#if !UNITY_EDITOR && UNITY_IOS
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsWebGL
        {
            get
            {
#if !UNITY_EDITOR && UNITY_WEBGL
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsWXWebGL
        {
            get
            {
#if !UNITY_EDITOR && UNITY_WEBGL && (WXWEBGL || MINIGAME_SUBPLATFORM_WEIXIN)
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsTTWebGL
        {
            get
            {
#if !UNITY_EDITOR && UNITY_WEBGL && (TTWEBGL || MINIGAME_SUBPLATFORM_DOUYIN)
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsMTWebGL
        {
            get
            {
#if !UNITY_EDITOR && UNITY_WEBGL && MTWEBGL
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsZFBWebGL
        {
            get
            {
#if !UNITY_EDITOR && UNITY_WEBGL && ZFBWEBGL
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsHMWebGL
        {
            get
            {
#if !UNITY_EDITOR && UNITY_WEBGL && HMWEBGL
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsHarmony
        {
            get
            {
#if !UNITY_EDITOR && UNITY_OPENHARMONY
                return true;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public bool IsInstanceGame
        {
            get
            {
#if !UNITY_EDITOR && UNITY_WEBGL && INSTANCE_GAME
                return true;
#else
                return false;
#endif
            }
        }
    }
}
