using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    
    /// <summary>
    /// 单个组件收集条目。
    /// 存储组件属性名和组件对象的映射关系。
    /// </summary>
    [System.Serializable]
    public class ComponentInfo
    {
        /// <summary>组件属性名，采用驼峰命名，如 rectCreate、imgCreate</summary>
        public string Name;

        /// <summary>组件对象</summary>
        public Component Obj;
    }
    
    /// <summary>
    /// UI 组件收集器数据。
    /// 挂载到 UI 根节点上，保存编辑器收集到的组件引用与生成路径。
    /// </summary>
    public class UIEntity : MonoBehaviour
    {
        /// <summary>组件收集列表</summary>
        public List<ComponentInfo> ComponentList = new List<ComponentInfo>();

        /// <summary>由组件生成的类的存放路径（相对于 Assets）</summary>
        public string GeneratedClassPath = "Scripts/UI/Generated";
    }
}
