using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GameEngine;
using UnityEditor;
using UnityEngine;

namespace GameEngineEditor
{
    /// <summary>
    /// UIComponentCollector 的自定义 Inspector。
    /// 展示完整的组件列表（可编辑 Name 和 Obj），提供"收集组件"和"生成代码"按钮。
    /// </summary>
    [CustomEditor(typeof(UIComponentCollector))]
    public class UIComponentCollectorInspector : UnityEditor.Editor
    {
        // ======================================================================
        //  SerializedProperties
        // ======================================================================

        private SerializedProperty _componentListProp;
        private SerializedProperty _generatedClassPathProp;

        // 代码生成模板
        private const string DefaultClassTemplate =
@"// 自动生成，请勿手动修改
// 生成时间：{GenTime}
using UnityEngine;
using UnityEngine.UI;

namespace GameEngine.Ui
{{
    public partial class {ClassName} : UIComponentCollector
    {{
{Properties}
    }}
}}
";

        private const string DefaultPropertyTemplate =
@"        public {TypeName} {PropertyName};";

        // ======================================================================
        //  生命周期
        // ======================================================================

        private void OnEnable()
        {
            _componentListProp = serializedObject.FindProperty(nameof(UIComponentCollector.ComponentList));
            _generatedClassPathProp = serializedObject.FindProperty(nameof(UIComponentCollector.GeneratedClassPath));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UIComponentCollector collector = (UIComponentCollector)target;

            // ── 标题 ──
            EditorUIHelper.DrawSectionHeader("UI 组件收集器");

            // ── 生成路径 ──
            EditorGUILayout.PropertyField(_generatedClassPathProp, new GUIContent("代码生成路径"));

            EditorGUILayout.Space(4);

            // ── 操作按钮 ──
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("收集组件", GUILayout.Height(30)))
            {
                var allRules = GetAllBindRulesFromAssetDatabase();
                collector.CollectComponents(allRules);
                EditorUtility.SetDirty(collector);
            }

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("生成代码", GUILayout.Height(30)))
            {
                GenerateCode(collector);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // ── 清理无效按钮 ──
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("清理无效引用", GUILayout.Height(20)))
            {
                CleanInvalidReferences(collector);
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // ── 组件列表 ──
            EditorUIHelper.DrawSectionHeader($"组件列表（共 {_componentListProp.arraySize} 项）");

            if (_componentListProp.arraySize == 0)
            {
                EditorUIHelper.DrawHelpBox("暂无组件。请先设置绑定配置并点击 [收集组件]，或手动添加。", MessageType.Info);
            }

            DrawComponentList();

            serializedObject.ApplyModifiedProperties();
        }

        // ======================================================================
        //  组件列表绘制
        // ======================================================================

        /// <summary>
        /// 绘制组件列表，每项显示序号、Name 编辑框、Obj 拖拽字段和删除按钮。
        /// </summary>
        private void DrawComponentList()
        {
            int deleteIndex = -1;

            for (int i = 0; i < _componentListProp.arraySize; i++)
            {
                SerializedProperty element = _componentListProp.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = element.FindPropertyRelative("Name");
                SerializedProperty objProp = element.FindPropertyRelative("Obj");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                // 序号
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));

                // 属性名
                EditorGUILayout.LabelField("Name", GUILayout.Width(40));
                EditorGUILayout.PropertyField(nameProp, GUIContent.none, GUILayout.MinWidth(100));

                // 组件对象
                EditorGUILayout.LabelField("Obj", GUILayout.Width(25));
                EditorGUILayout.PropertyField(objProp, GUIContent.none, GUILayout.MinWidth(150));

                // 删除按钮
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("X", GUILayout.Width(25), GUILayout.Height(16)))
                {
                    deleteIndex = i;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(1);
            }

            // 执行删除
            if (deleteIndex >= 0)
            {
                _componentListProp.DeleteArrayElementAtIndex(deleteIndex);
            }

            // 手动添加按钮
            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ 手动添加组件"))
            {
                _componentListProp.arraySize++;
            }
        }

        // ======================================================================
        //  代码生成
        // ======================================================================

        /// <summary>
        /// 根据组件列表和配置模板生成绑定代码文件。
        /// </summary>
        private void GenerateCode(UIComponentCollector collector)
        {
            if (collector.ComponentList == null || collector.ComponentList.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "组件列表为空，请先收集组件。", "确定");
                return;
            }

            // 构建属性代码块
            StringBuilder propertiesBuilder = new StringBuilder();
            Dictionary<string, string> eventMethods = new Dictionary<string, string>();

            for (int i = 0; i < collector.ComponentList.Count; i++)
            {
                ComponentInfo info = collector.ComponentList[i];
                if (info == null || info.Obj == null) continue;

                string propertyName = info.Name;
                string typeName = GetTypeNameForComponent(info.Obj);

                // 添加属性声明
                propertiesBuilder.AppendLine(string.Format(DefaultPropertyTemplate, typeName, propertyName));

                // 查找对应的绑定规则以生成事件绑定代码
                BindRule rule = FindRuleForProperty(collector, info);
                if (rule != null)
                {
                    // 如果有 TemplateCode1（事件绑定代码），生成
                    if (!string.IsNullOrEmpty(rule.TemplateCode1))
                    {
                        propertiesBuilder.AppendLine();
                        propertiesBuilder.AppendLine(ReplaceTemplate(rule.TemplateCode1, propertyName, collector.name));
                    }

                    // 如果有 TemplateCode2（事件绑定方法），收集
                    if (!string.IsNullOrEmpty(rule.TemplateCode2))
                    {
                        string methodCode = ReplaceTemplate(rule.TemplateCode2, propertyName, collector.name);
                        string methodKey = $"Method_{propertyName}";
                        if (!eventMethods.ContainsKey(methodKey))
                        {
                            eventMethods[methodKey] = methodCode;
                        }
                    }
                }
            }

            // 如果有事件方法模板，添加方法区域
            if (eventMethods.Count > 0)
            {
                propertiesBuilder.AppendLine();
                propertiesBuilder.AppendLine("        #region 事件绑定方法");
                foreach (var kvp in eventMethods)
                {
                    propertiesBuilder.AppendLine(kvp.Value);
                }
                propertiesBuilder.AppendLine("        #endregion");
            }

            // 生成完整类代码
            string className = collector.name.Replace(" ", "").Replace("(", "").Replace(")", "");
            string classCode = DefaultClassTemplate
                .Replace("{ClassName}", className)
                .Replace("{GenTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("{Properties}", propertiesBuilder.ToString());

            // 写入文件
            string folder = Path.Combine(Application.dataPath, collector.GeneratedClassPath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            string filePath = Path.Combine(folder, $"{className}.cs");
            string message;

            if (File.Exists(filePath))
            {
                File.WriteAllText(filePath, classCode, Encoding.UTF8);
                message = $"代码已覆盖写入：{filePath}\n\n内容共 {classCode.Length} 字符。";
            }
            else
            {
                File.WriteAllText(filePath, classCode, Encoding.UTF8);
                message = $"代码已生成：{filePath}\n\n内容共 {classCode.Length} 字符。";
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("生成完成", message, "确定");
            Debug.Log($"[UIComponentCollectorInspector] 代码已生成: {filePath}");
        }

        /// <summary>
        /// 获取组件对应的类型全名。
        /// </summary>
        private string GetTypeNameForComponent(Component comp)
        {
            if (comp == null) return "Component";
            return comp.GetType().FullName;
        }

        /// <summary>
        /// 从项目中通过 AssetDatabase 加载所有 ComponentBindConfig，合并返回所有绑定规则。
        /// </summary>
        private static List<BindRule> GetAllBindRulesFromAssetDatabase()
        {
            var result = new List<BindRule>();
            var mergedRules = new Dictionary<string, BindRule>();

            string[] guids = AssetDatabase.FindAssets("t:ComponentBindConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<ComponentBindConfig>(path);
                if (config == null || config.Rules == null) continue;

                foreach (var rule in config.Rules)
                {
                    if (rule != null && !string.IsNullOrEmpty(rule.Prefix) && !mergedRules.ContainsKey(rule.Prefix))
                    {
                        mergedRules[rule.Prefix] = rule;
                        result.Add(rule);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 根据 ComponentInfo 的属性名反查绑定的规则。
        /// </summary>
        private BindRule FindRuleForProperty(UIComponentCollector collector, ComponentInfo info)
        {
            var allRules = GetAllBindRulesFromAssetDatabase();
            if (allRules.Count == 0) return null;

            // 尝试通过节点解析反查：遍历所有子节点找到匹配 component 来定位对应的前缀
            var allTransforms = collector.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t == collector.transform) continue;

                string nodeName = t.name;
                var bindings = collector.ParseNodeName(nodeName);
                if (bindings == null) continue;

                foreach (var binding in bindings)
                {
                    string expectedPropName = UIComponentCollector.GeneratePropertyName(binding.Prefix, binding.NodeName);
                    if (expectedPropName == info.Name)
                    {
                        // 在所有规则中查找匹配的 Prefix
                        foreach (var rule in allRules)
                        {
                            if (rule.Prefix == binding.Prefix)
                                return rule;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 替换模板中的占位符。
        /// 占位符约定：
        ///   {PropertyName} → 属性名
        ///   {TypeName}      → 类型名
        ///   {GameObjectName} → 所属 GameObject 名称
        /// </summary>
        private string ReplaceTemplate(string template, string propertyName, string gameObjectName)
        {
            if (string.IsNullOrEmpty(template)) return template;

            return template
                .Replace("{PropertyName}", propertyName)
                .Replace("{TypeName}", "Component")
                .Replace("{GameObjectName}", gameObjectName);
        }

        // ======================================================================
        //  清理
        // ======================================================================

        /// <summary>
        /// 清理无效的组件引用（Obj 为 null 的条目）。
        /// </summary>
        private void CleanInvalidReferences(UIComponentCollector collector)
        {
            int removed = collector.ComponentList.RemoveAll(item => item == null || item.Obj == null);
            if (removed > 0)
            {
                EditorUtility.SetDirty(collector);
                Debug.Log($"[UIComponentCollectorInspector] 已清理 {removed} 条无效引用。");
            }
            else
            {
                Debug.Log("[UIComponentCollectorInspector] 没有无效引用需要清理。");
            }
        }
    }
}