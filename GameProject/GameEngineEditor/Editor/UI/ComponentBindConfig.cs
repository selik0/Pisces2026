using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 单个绑定规则：组件缩写 -> 组件类型 + 代码模板
    /// </summary>
    [Serializable]
    public class BindRule
    {
        /// <summary>组件缩写，如 rect、img</summary>
        public string Prefix;

        /// <summary>同名前缀的匹配优先级，数值越大越优先；相同时按配置顺序匹配。</summary>
        public int Priority;

        /// <summary>组件类型，包含完整命名空间，如 UnityEngine.UI.Image</summary>
        public string ClassName;

        /// <summary>true 表示自动收集，false 表示根据命名规则收集</summary>
        public bool Auto;

        /// <summary>直接插入 OnBind 的 UI 事件注册语句，{EventName} 为去掉前缀且首字母大写的组件名称。</summary>
        [TextArea(3, 10)]
        public string TemplateCode1;

        /// <summary>事件回调方法模板，可通过 partial void 声明将业务实现放入独立逻辑文件。</summary>
        [TextArea(3, 10)]
        public string TemplateCode2;

        /// <summary>直接插入 OnUnbind 的 UI 事件注销语句。</summary>
        [TextArea(3, 10)]
        public string TemplateCode3;
    }

    /// <summary>
    /// UI 组件绑定配置 ScriptableObject。
    /// 通过配置 Prefix 缩写与 ClassName 的映射关系，实现节点命名到组件的自动绑定。
    /// </summary>
    [CreateAssetMenu(menuName = "GameEngine/UI Component Bind Config", fileName = "ComponentBindConfig")]
    public class ComponentBindConfig : ScriptableObject
    {
        /// <summary>绑定规则列表</summary>
        public List<BindRule> Rules = new List<BindRule>();

        /// <summary>
        /// 根据组件缩写查找最高优先级规则对应的 ClassName。
        /// </summary>
        /// <param name="prefix">组件缩写</param>
        /// <returns>完整类型名，未找到返回 null</returns>
        public string GetClassName(string prefix)
        {
            return GetRule(prefix)?.ClassName;
        }

        /// <summary>
        /// 根据组件缩写查找最高优先级绑定规则；优先级相同时返回配置中较早的规则。
        /// </summary>
        public BindRule GetRule(string prefix)
        {
            if (Rules == null)
            {
                return null;
            }

            BindRule selected = null;
            foreach (var rule in Rules)
            {
                if (rule != null && rule.Prefix == prefix &&
                    (selected == null || rule.Priority > selected.Priority))
                {
                    selected = rule;
                }
            }
            return selected;
        }
    }
}
