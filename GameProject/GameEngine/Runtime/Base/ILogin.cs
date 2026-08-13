namespace GameEngine
{
    /// <summary>
    /// 定义登录对象的基本能力。
    /// </summary>
    public interface ILogin
    {
        bool IsLoggedIn { get; }

        void Login();

        void Logout();
    }
}
