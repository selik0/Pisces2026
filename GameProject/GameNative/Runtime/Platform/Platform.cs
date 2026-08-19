namespace GameNative
{
    /// <summary>
    /// 平台判断静态入口，继承 <see cref="ServiceBase{T}"/> 获得依赖注入能力。
    ///
    /// <para>
    /// 业务代码统一通过本类判断平台（例如 <c>Platform.IsAndroid</c>），
    /// 由宿主工程启动阶段调用 <see cref="ServiceBase{T}.SetService"/> 注入实现；
    /// 未注入前所有判断均返回 false。
    /// </para>
    /// </summary>
    public sealed class Platform : ServiceBase<IPlatform>
    {
        private Platform()
        {
        }

        /// <summary>是否运行在 PC 真机。</summary>
        public static bool IsPC => HasService && Service.IsPC;

        /// <summary>是否运行在编辑器。</summary>
        public static bool IsEditor => HasService && Service.IsEditor;

        /// <summary>是否运行在 Android 编辑器。</summary>
        public static bool IsAndroidEditor => HasService && Service.IsAndroidEditor;

        /// <summary>是否运行在 iOS 编辑器。</summary>
        public static bool IsIOSEditor => HasService && Service.IsIOSEditor;

        /// <summary>是否运行在 PC 真机或编辑器。</summary>
        public static bool IsPCOrEditor => HasService && Service.IsPCOrEditor;

        /// <summary>是否运行在 Android 真机。</summary>
        public static bool IsAndroid => HasService && Service.IsAndroid;

        /// <summary>是否运行在 iOS 真机。</summary>
        public static bool IsIOS => HasService && Service.IsIOS;

        /// <summary>是否运行在 WebGL 真机。</summary>
        public static bool IsWebGL => HasService && Service.IsWebGL;

        /// <summary>是否运行在微信小游戏。</summary>
        public static bool IsWXWebGL => HasService && Service.IsWXWebGL;

        /// <summary>是否运行在抖音小游戏。</summary>
        public static bool IsTTWebGL => HasService && Service.IsTTWebGL;

        /// <summary>是否运行在美团小游戏。</summary>
        public static bool IsMTWebGL => HasService && Service.IsMTWebGL;

        /// <summary>是否运行在支付宝小游戏。</summary>
        public static bool IsZFBWebGL => HasService && Service.IsZFBWebGL;

        /// <summary>是否运行在华为小游戏。</summary>
        public static bool IsHMWebGL => HasService && Service.IsHMWebGL;

        /// <summary>是否运行在鸿蒙 HarmonyOS。</summary>
        public static bool IsHarmony => HasService && Service.IsHarmony;

        /// <summary>是否运行在快游戏（OPPO/vivo/华为等厂商快游戏）。</summary>
        public static bool IsInstanceGame => HasService && Service.IsInstanceGame;
    }
}
