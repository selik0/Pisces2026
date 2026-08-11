using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using GameEngine;

namespace GameEngineEditor
{
    /// <summary>
    /// 节点名称解析结果，仅用于编辑器组件收集流程。
    /// </summary>
    public class NodeNameBinding
    {
        /// <summary>组件缩写前缀</summary>
        public string Prefix;

        /// <summary>节点名称部分</summary>
        public string NodeName;
    }

    /// <summary>
    /// UIEntity 的编辑器辅助类，负责组件收集逻辑。
    /// </summary>
    public static class UIEntityHelper
    {
        /// <summary>
        /// 根据给定的绑定规则列表，自动收集目标对象所有子节点的组件。
        /// </summary>
        /// <param name="collector">目标 UIEntity 实例</param>
        /// <param name="rules">绑定规则列表</param>
        public static void CollectComponents(UIEntity collector, List<BindRule> rules)
        {
            if (rules == null || rules.Count == 0)
            {
                Debug.LogWarning("[UIEntity] 绑定规则列表为空。");
                return;
            }

            // ── 预处理：构建规则表，预解析类型 ──
            var namedRules = new Dictionary<string, BindRule>();    // prefix → rule（用于命名规则匹配）
            var autoRules = new List<BindRule>();                   // Auto=true 的规则
            var typeCache = new Dictionary<string, Type>();         // className → Type（避免重复反射）

            foreach (var rule in rules)
            {
                if (rule == null || string.IsNullOrEmpty(rule.ClassName) || string.IsNullOrEmpty(rule.Prefix))
                {
                    continue;
                }

                // 预解析类型（每个 ClassName 仅反射一次）
                if (!typeCache.ContainsKey(rule.ClassName))
                {
                    Type type = ResolveType(rule.ClassName);
                    if (type != null)
                    {
                        typeCache[rule.ClassName] = type;
                    }
                }

                if (rule.Auto)
                {
                    autoRules.Add(rule);
                }

                if (!namedRules.ContainsKey(rule.Prefix))
                {
                    namedRules[rule.Prefix] = rule;
                }
            }

            collector.ComponentList.Clear();
            var addedNames = new HashSet<string>();
            var allTransforms = collector.GetComponentsInChildren<Transform>(true);

            // ── 单次遍历所有子节点 ──
            foreach (var t in allTransforms)
            {
                if (t == collector.transform)
                {
                    continue;
                }

                string nodeName = t.name;
                var bindings = ParseNodeName(nodeName);

                // 1. 按命名规则收集
                if (bindings != null)
                {
                    foreach (var binding in bindings)
                    {
                        if (!namedRules.TryGetValue(binding.Prefix, out var rule))
                        {
                            continue;
                        }

                        string propertyName = GeneratePropertyName(binding.Prefix, binding.NodeName);
                        CollectFromTransform(collector, t, rule, propertyName, typeCache, addedNames);
                    }
                }

                // 2. 自动收集
                foreach (var autoRule in autoRules)
                {
                    string propertyName = BuildAutoPropertyName(nodeName, autoRule.Prefix);
                    CollectAllFromTransform(collector, t, autoRule, propertyName, typeCache, addedNames);
                }
            }

            Debug.Log($"[UIEntity] 收集完成，共 {collector.ComponentList.Count} 个组件。");
        }

        /// <summary>
        /// 从 Transform 上获取单个命名规则对应的组件并添加。
        /// </summary>
        private static void CollectFromTransform(UIEntity collector, Transform t, BindRule rule, string propertyName,
            Dictionary<string, Type> typeCache, HashSet<string> addedNames)
        {
            if (!addedNames.Add(propertyName))
            {
                return;
            }

            if (!typeCache.TryGetValue(rule.ClassName, out var type))
            {
                Debug.LogWarning($"[UIEntity] 无法解析类型 '{rule.ClassName}'，节点: {t.name}");
                return;
            }

            Component comp = t.GetComponent(type);
            if (comp == null)
            {
                Debug.LogWarning($"[UIEntity] 节点 '{t.name}' 上未找到类型 '{rule.ClassName}' 的组件");
                return;
            }

            collector.ComponentList.Add(new ComponentInfo { Name = propertyName, Obj = comp });
        }

        /// <summary>
        /// 从 Transform 上获取所有 Auto 规则指定类型的组件并添加。
        /// 支持同一节点存在多个同类型组件（自动追加数字后缀）。
        /// </summary>
        private static void CollectAllFromTransform(UIEntity collector, Transform t, BindRule rule, string basePropertyName,
            Dictionary<string, Type> typeCache, HashSet<string> addedNames)
        {
            if (!typeCache.TryGetValue(rule.ClassName, out var type))
            {
                return;
            }

            Component[] comps = t.GetComponents(type);
            if (comps == null || comps.Length == 0)
            {
                return;
            }

            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null)
                {
                    continue;
                }

                // 多个同类型组件时自动追加数字后缀，如 btnConfirm、btnConfirm2
                string finalName = comps.Length > 1 ? basePropertyName + (i + 1) : basePropertyName;

                if (!addedNames.Add(finalName))
                {
                    continue;
                }

                collector.ComponentList.Add(new ComponentInfo { Name = finalName, Obj = comps[i] });
            }
        }

        /// <summary>
        /// 根据节点名和前缀生成自动收集模式的属性名。
        /// 取最后一个下划线后字段作为名称部分；如无下划线则用完整节点名。
        /// </summary>
        private static string BuildAutoPropertyName(string nodeName, string prefix)
        {
            int lastUnderscore = nodeName.LastIndexOf('_');
            string nameField = lastUnderscore >= 0 ? nodeName.Substring(lastUnderscore + 1) : nodeName;
            return GeneratePropertyName(prefix, nameField);
        }

        /// <summary>
        /// 在所有已加载的程序集中通过完整类名解析类型。
        /// </summary>
        /// <param name="className">完整类名（含命名空间）</param>
        /// <returns>解析到的类型，未找到返回 null</returns>
        public static Type ResolveType(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                return null;
            }

            // 先尝试直接获取
            Type type = Type.GetType(className);
            if (type != null)
            {
                return type;
            }

            // 遍历所有已加载的程序集
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                type = asm.GetType(className);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        /// <summary>
        /// 解析节点名称，提取组件缩写和节点名称。
        /// 如 "rect_img_create" → [ (rect, create), (img, create) ]
        /// </summary>
        /// <param name="nodeName">节点原始名称</param>
        /// <returns>解析后的绑定信息列表</returns>
        public static List<NodeNameBinding> ParseNodeName(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName))
            {
                return null;
            }

            string[] parts = nodeName.Split('_');
            if (parts.Length < 2)
            {
                return null; // 至少需要一个缩写前缀和一个节点名
            }

            // 最后一个字段是节点名称
            string nameField = parts[parts.Length - 1];

            // 前面的字段都是组件缩写
            var result = new List<NodeNameBinding>();
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string prefix = parts[i];
                if (string.IsNullOrEmpty(prefix))
                {
                    continue;
                }

                result.Add(new NodeNameBinding
                {
                    Prefix = prefix,
                    NodeName = nameField
                });
            }

            return result.Count > 0 ? result : null;
        }

        /// <summary>
        /// 生成组件属性名：组件缩写 + 驼峰命名的节点名。
        /// 如 prefix="rect", nodeName="create" → "rectCreate"
        /// </summary>
        public static string GeneratePropertyName(string prefix, string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName))
            {
                return prefix;
            }

            // 节点名转驼峰：首字母大写，其余保持
            string camelNodeName = char.ToUpper(nodeName[0]) + nodeName.Substring(1);
            return prefix + camelNodeName;
        }
    }
}
