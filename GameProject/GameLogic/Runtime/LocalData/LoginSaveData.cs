using System;
using GameEngine;

namespace GameLogic
{
    /// <summary>设备上最近一次成功登录的账号和密码，不区分角色。</summary>
    [Serializable]
    public sealed class LoginSaveData : BaseSaveData
    {
        public string Account = string.Empty;
        public string Password = string.Empty;
    }
}
