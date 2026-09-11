using System;

namespace GameNative
{
    /// <summary>
    /// 平台判断静态入口。
    ///
    /// <para>
    /// 业务代码统一通过本类判断平台（例如 <c>Platform.IsAndroid</c>），
    /// 由宿主工程启动阶段调用 <see cref="SetService"/> 注入实现；
    /// 未注入前所有判断均返回 false。
    /// </para>
    /// </summary>
    public static class Platform
    {
        private static IPlatform _service;

        private static bool HasService => _service != null;

        /// <summary>注入宿主提供的平台服务，重复调用会覆盖旧实现。</summary>
        /// <param name="service">平台服务实现，不能为 null。</param>
        public static void SetService(IPlatform service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), "Platform service implementation cannot be null.");
            }

            _service = service;
        }

        /// <summary>清空注入的平台服务。</summary>
        public static void ClearService()
        {
            _service = null;
        }

        /// <summary>是否运行在 PC 真机。</summary>
        public static bool IsPC => HasService && _service.IsPC;

        /// <summary>是否运行在编辑器。</summary>
        public static bool IsEditor => HasService && _service.IsEditor;

        /// <summary>是否运行在 Android 编辑器。</summary>
        public static bool IsAndroidEditor => HasService && _service.IsAndroidEditor;

        /// <summary>是否运行在 iOS 编辑器。</summary>
        public static bool IsIOSEditor => HasService && _service.IsIOSEditor;

        /// <summary>是否运行在 PC 真机或编辑器。</summary>
        public static bool IsPCOrEditor => HasService && _service.IsPCOrEditor;

        /// <summary>是否运行在 Android 真机。</summary>
        public static bool IsAndroid => HasService && _service.IsAndroid;

        /// <summary>是否运行在 iOS 真机。</summary>
        public static bool IsIOS => HasService && _service.IsIOS;

        /// <summary>是否运行在 WebGL 真机。</summary>
        public static bool IsWebGL => HasService && _service.IsWebGL;

        /// <summary>是否运行在微信小游戏。</summary>
        public static bool IsWXWebGL => HasService && _service.IsWXWebGL;

        /// <summary>是否运行在抖音小游戏。</summary>
        public static bool IsTTWebGL => HasService && _service.IsTTWebGL;

        /// <summary>是否运行在美团小游戏。</summary>
        public static bool IsMTWebGL => HasService && _service.IsMTWebGL;

        /// <summary>是否运行在支付宝小游戏。</summary>
        public static bool IsZFBWebGL => HasService && _service.IsZFBWebGL;

        /// <summary>是否运行在华为小游戏。</summary>
        public static bool IsHMWebGL => HasService && _service.IsHMWebGL;

        /// <summary>是否运行在鸿蒙 HarmonyOS。</summary>
        public static bool IsHarmony => HasService && _service.IsHarmony;

        /// <summary>是否运行在快游戏（OPPO/vivo/华为等厂商快游戏）。</summary>
        public static bool IsInstanceGame => HasService && _service.IsInstanceGame;
    }
}
