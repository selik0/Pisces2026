using System;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 单个组件收集条目。
    /// 存储组件属性名和组件对象的映射关系。
    /// </summary>
    [Serializable]
    public class ComponentInfo
    {
        /// <summary>组件属性名，采用驼峰命名，如 rectCreate、imgCreate</summary>
        public string Name;

        /// <summary>组件对象</summary>
        public Component Obj;
    }
}