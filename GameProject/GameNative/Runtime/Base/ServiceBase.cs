using System;

namespace GameNative
{
    /// <summary>
    /// 依赖注入泛型基类。
    /// 静态注入槽位按服务接口类型 <typeparamref name="T"/> 隔离，
    /// 派生类通过继承获得统一的注入与读取能力：
    /// 宿主工程启动阶段调用 <see cref="SetService"/> 注入实现，业务代码通过 <see cref="Service"/> 读取。
    /// </summary>
    /// <typeparam name="T">服务接口类型。</typeparam>
    public abstract class ServiceBase<T> where T : class
    {
        private static T _service;

        /// <summary>当前注入的服务实现，未注入时为 null。</summary>
        public static T Service => _service;

        /// <summary>是否已注入服务。</summary>
        public static bool HasService => _service != null;

        /// <summary>
        /// 注入服务实现（依赖注入装配点），重复调用会覆盖旧实现。
        /// </summary>
        /// <param name="service">服务实现，不能为 null。</param>
        public static void SetService(T service)
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service), "Service implementation cannot be null.");
            }

            _service = service;
        }

        /// <summary>清空注入的服务（测试用，游戏运行时慎用）。</summary>
        public static void ClearService()
        {
            _service = null;
        }
    }
}
