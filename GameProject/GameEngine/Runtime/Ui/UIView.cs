using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// UI 界面基类。
    /// 提供生命周期管理、GameObject 绑定、组件绑定、事件注册/反注册等基础功能。
    /// 子类通过重写 OnXxx 方法实现具体逻辑。
    /// </summary>
    public abstract class UIView : UIBrick
    {
        /// <summary>UI 所属层级</summary>
        public UILayer Layer { get; private set; }
    }
}