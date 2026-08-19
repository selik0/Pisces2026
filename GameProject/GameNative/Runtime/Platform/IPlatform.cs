namespace GameNative
{
    /// <summary>
    /// 平台判断抽象（控制反转）。
    /// 框架层只依赖本接口判断运行平台，不感知具体宏和引擎 API；
    /// 宿主工程提供实现并通过 <see cref="ServiceBase{T}.SetService"/> 注入。
    /// </summary>
    public interface IPlatform
    {
        /// <summary>是否运行在 PC 真机（!UNITY_EDITOR &amp;&amp; UNITY_STANDALONE）。</summary>
        bool IsPC { get; }

        /// <summary>是否运行在编辑器（UNITY_EDITOR，任意构建目标）。</summary>
        bool IsEditor { get; }

        /// <summary>是否运行在 Android 编辑器（UNITY_EDITOR &amp;&amp; UNITY_ANDROID）。</summary>
        bool IsAndroidEditor { get; }

        /// <summary>是否运行在 iOS 编辑器（UNITY_EDITOR &amp;&amp; UNITY_IOS）。</summary>
        bool IsIOSEditor { get; }

        /// <summary>是否运行在 PC 真机或编辑器（UNITY_EDITOR || UNITY_STANDALONE）。</summary>
        bool IsPCOrEditor { get; }

        /// <summary>是否运行在 Android 真机（!UNITY_EDITOR &amp;&amp; UNITY_ANDROID）。</summary>
        bool IsAndroid { get; }

        /// <summary>是否运行在 iOS 真机（!UNITY_EDITOR &amp;&amp; UNITY_IOS）。</summary>
        bool IsIOS { get; }

        /// <summary>是否运行在 WebGL 真机（!UNITY_EDITOR &amp;&amp; UNITY_WEBGL）。</summary>
        bool IsWebGL { get; }

        /// <summary>是否运行在微信小游戏。</summary>
        bool IsWXWebGL { get; }

        /// <summary>是否运行在抖音小游戏。</summary>
        bool IsTTWebGL { get; }

        /// <summary>是否运行在美团小游戏。</summary>
        bool IsMTWebGL { get; }

        /// <summary>是否运行在支付宝小游戏。</summary>
        bool IsZFBWebGL { get; }

        /// <summary>是否运行在华为小游戏。</summary>
        bool IsHMWebGL { get; }

        /// <summary>是否运行在鸿蒙 HarmonyOS（UNITY_OPENHARMONY，团结引擎）。</summary>
        bool IsHarmony { get; }

        /// <summary>是否运行在快游戏（OPPO/vivo/华为等厂商快游戏）。</summary>
        bool IsInstanceGame { get; }
    }
}
