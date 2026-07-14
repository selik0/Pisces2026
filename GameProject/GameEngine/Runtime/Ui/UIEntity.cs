using System;
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

        // ── 组件访问 ──

        /// <summary>
        /// 从当前 GameObject 上获取组件，若不存在则添加。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件实例</returns>
        public T GetOrAddComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        /// <summary>
        /// 从当前 GameObject 的子节点上按路径查找组件。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="path">子节点路径（相对于当前 GameObject）</param>
        /// <returns>组件实例，未找到返回 null</returns>
        public T GetChildComponent<T>(string path = null) where T : Component
        {
            if (string.IsNullOrEmpty(path))
            {
                return GetComponentInChildren<T>();
            }

            Transform child = transform.Find(path);
            return child != null ? child.GetComponent<T>() : null;
        }

        /// <summary>
        /// 从当前 GameObject 的子节点上获取所有组件。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <returns>组件数组</returns>
        public T[] GetChildComponents<T>() where T : Component
        {
            return GetComponentsInChildren<T>();
        }

        /// <summary>
        /// 按收集时生成的属性名获取组件。
        /// </summary>
        /// <typeparam name="T">组件类型</typeparam>
        /// <param name="name">组件属性名</param>
        /// <returns>组件实例，未找到或类型不匹配时返回 null</returns>
        public T GetCollectedComponent<T>(string name) where T : Component
        {
            if (string.IsNullOrEmpty(name) || ComponentList == null) return null;

            for (int i = 0; i < ComponentList.Count; i++)
            {
                ComponentInfo info = ComponentList[i];
                if (info != null && info.Name == name)
                {
                    return info.Obj as T;
                }
            }

            return null;
        }

        /// <summary>
        /// GameObject 即将销毁时触发。
        /// </summary>
        private event Action DestroyAction;

        private void OnDestroy()
        {
            DestroyAction?.Invoke();
            DestroyAction = null;
        }

        public void AddDestroyListener(Action action)
        {
            DestroyAction += action;
        }

        public void RemoveDestroyListener(Action action)
        {
            DestroyAction -= action;
        }
    }
}
