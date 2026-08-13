namespace GameEngine
{
    /// <summary>
    /// 普通类型的线程安全单例基类。
    /// 派生类必须提供公开的无参构造函数。
    /// </summary>
    public abstract class Singleton<T> : ILogin where T : Singleton<T>, new()
    {
        private static readonly T _instance = new T();

        public static T Instance => _instance;

        public virtual bool IsLoggedIn { get; protected set; }

        protected Singleton()
        {
        }

        public virtual void Login()
        {
            IsLoggedIn = true;
        }

        public virtual void Logout()
        {
            IsLoggedIn = false;
        }
    }
}
