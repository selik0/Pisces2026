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
                UIComponentCollectorHelper.CollectComponents(collector, allRules);
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
        /// 绘制组件列表，每项显示序号、Name 编辑框、Obj 下拉框（列出该组件所在节点的所有组件）和删除按钮。
        /// 当修改 Obj 后，属性名会根据绑定规则自动更新。若存在同名属性名则输入框标红。
        /// </summary>
        private void DrawComponentList()
        {
            int deleteIndex = -1;

            // ── 预扫描：检测重复的属性名 ──
            var nameCounts = new Dictionary<string, int>();
            for (int i = 0; i < _componentListProp.arraySize; i++)
            {
                SerializedProperty element = _componentListProp.GetArrayElementAtIndex(i);
                string name = element.FindPropertyRelative("Name").stringValue;
                if (!string.IsNullOrEmpty(name))
                {
                    if (nameCounts.ContainsKey(name))
                        nameCounts[name]++;
                    else
                        nameCounts[name] = 1;
                }
            }

            // ── 构建类型到规则的映射表 ──
            var allRules = GetAllBindRulesFromAssetDatabase();
            var typeToRule = new Dictionary<string, BindRule>();
            foreach (var rule in allRules)
            {
                Type type = UIComponentCollectorHelper.ResolveType(rule.ClassName);
                if (type != null && !typeToRule.ContainsKey(type.FullName))
                {
                    typeToRule[type.FullName] = rule;
                }
            }

            UIComponentCollector collector = (UIComponentCollector)target;

            for (int i = 0; i < _componentListProp.arraySize; i++)
            {
                SerializedProperty element = _componentListProp.GetArrayElementAtIndex(i);
                SerializedProperty nameProp = element.FindPropertyRelative("Name");
                SerializedProperty objProp = element.FindPropertyRelative("Obj");

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();

                // 序号
                EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(30));

                // ── 属性名（同名则标红） ──
                EditorGUILayout.LabelField("Name", GUILayout.Width(40));

                string currentName = nameProp.stringValue;
                bool isDuplicate = !string.IsNullOrEmpty(currentName)
                    && nameCounts.TryGetValue(currentName, out int cnt) && cnt > 1;

                Color oldBgColor = GUI.backgroundColor;
                if (isDuplicate)
                {
                    GUI.backgroundColor = new Color(1f, 0.35f, 0.35f, 1f);
                }
                EditorGUILayout.PropertyField(nameProp, GUIContent.none, GUILayout.MinWidth(100));
                GUI.backgroundColor = oldBgColor;

                // ── 组件对象：下拉框选择该组件所在节点的所有组件 ──
                EditorGUILayout.LabelField("Obj", GUILayout.Width(25));

                Component prevObj = objProp.objectReferenceValue as Component;
                DrawComponentDropdown(objProp, prevObj);

                // 检测组件是否发生变化，若变化则自动更新属性名
                Component newObj = objProp.objectReferenceValue as Component;
                if (newObj != prevObj && newObj != null)
                {
                    AutoUpdatePropertyName(nameProp, newObj, typeToRule, collector);
                }

                // ── 删除按钮 ──
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
        //  组件下拉框
        // ======================================================================

        /// <summary>
        /// 绘制组件下拉框。若当前 Obj 为空则回退到标准 ObjectField；
        /// 否则列出该组件所在 GameObject 上的所有组件供选择。
        /// </summary>
        private void DrawComponentDropdown(SerializedProperty objProp, Component currentObj)
        {
            if (currentObj == null)
            {
                // 没有现有组件引用时使用标准拖拽字段
                EditorGUILayout.PropertyField(objProp, GUIContent.none, GUILayout.MinWidth(150));
                return;
            }

            GameObject go = currentObj.gameObject;
            Component[] allComponents = go.GetComponents<Component>();

            // 构建下拉选项（首项为 None）
            string[] displayNames = new string[allComponents.Length + 1];
            displayNames[0] = "None";
            int selectedIndex = 0;

            for (int j = 0; j < allComponents.Length; j++)
            {
                displayNames[j + 1] = allComponents[j].GetType().Name;
                if (allComponents[j] == currentObj)
                    selectedIndex = j + 1;
            }

            int newIndex = EditorGUILayout.Popup(selectedIndex, displayNames, GUILayout.MinWidth(150));
            if (newIndex != selectedIndex)
            {
                objProp.objectReferenceValue = (newIndex == 0) ? null : (UnityEngine.Object)allComponents[newIndex - 1];
            }
        }

        // ======================================================================
        //  属性名自动更新
        // ======================================================================

        /// <summary>
        /// 根据新选中的组件，查找匹配的 BindRule 并结合节点名称自动更新属性名。
        /// 若无法匹配到规则则保持原有名称不变。
        /// </summary>
        private void AutoUpdatePropertyName(SerializedProperty nameProp, Component obj,
            Dictionary<string, BindRule> typeToRule, UIComponentCollector collector)
        {
            if (obj == null) return;

            string typeFullName = obj.GetType().FullName;
            if (!typeToRule.TryGetValue(typeFullName, out BindRule rule)) return;

            string nodeName = obj.gameObject.name;
            var bindings = UIComponentCollectorHelper.ParseNodeName(nodeName);

            string propertyName;
            if (bindings != null && bindings.Count > 0)
            {
                // 从节点名解析结果中取第一个绑定的节点名部分
                propertyName = UIComponentCollectorHelper.GeneratePropertyName(rule.Prefix, bindings[0].NodeName);
            }
            else
            {
                // 节点名无下划线格式时直接使用完整节点名
                propertyName = UIComponentCollectorHelper.GeneratePropertyName(rule.Prefix, nodeName);
            }

            nameProp.stringValue = propertyName;
        }

        // ======================================================================
        //  代码生成
        // ======================================================================

        /// <summary>
        /// 根据组件列表和配置模板生成绑定代码文件。
        /// 生成前检查同名属性和组件引用丢失，有问题时弹窗确认。
        /// </summary>
        private void GenerateCode(UIComponentCollector collector)
        {
            if (collector.ComponentList == null || collector.ComponentList.Count == 0)
            {
                EditorUtility.DisplayDialog("提示", "组件列表为空，请先收集组件。", "确定");
                return;
            }

            // ── 生成前检查 ──
            List<string> warnings = new List<string>();

            // 1. 检查同名属性
            var nameCounts = new Dictionary<string, int>();
            foreach (var info in collector.ComponentList)
            {
                if (info == null || string.IsNullOrEmpty(info.Name)) continue;
                if (nameCounts.ContainsKey(info.Name))
                    nameCounts[info.Name]++;
                else
                    nameCounts[info.Name] = 1;
            }

            var duplicates = new List<string>();
            foreach (var kvp in nameCounts)
            {
                if (kvp.Value > 1)
                    duplicates.Add($"  · {kvp.Key}（出现 {kvp.Value} 次）");
            }
            if (duplicates.Count > 0)
            {
                warnings.Add($"存在同名属性（{duplicates.Count} 个）：\n{string.Join("\n", duplicates)}");
            }

            // 2. 检查组件引用丢失
            int nullObjCount = 0;
            int nullInfoCount = 0;
            for (int i = 0; i < collector.ComponentList.Count; i++)
            {
                var info = collector.ComponentList[i];
                if (info == null)
                {
                    nullInfoCount++;
                }
                else if (info.Obj == null)
                {
                    nullObjCount++;
                }
            }
            if (nullInfoCount > 0 || nullObjCount > 0)
            {
                string msg = "存在组件引用丢失：";
                if (nullObjCount > 0)
                    msg += $"\n  · {nullObjCount} 个条目对组件引用为 null（Obj 为空）";
                if (nullInfoCount > 0)
                    msg += $"\n  · {nullInfoCount} 个条目为 null（缺失条目）";
                msg += "\n\n丢失引用的条目将被跳过，不会生成对应属性。";
                warnings.Add(msg);
            }

            // 有警告则弹窗确认
            if (warnings.Count > 0)
            {
                string title = "生成代码 - 警告";
                string allWarnings = string.Join("\n\n", warnings);
                allWarnings += "\n\n是否继续生成代码？";

                if (!EditorUtility.DisplayDialog(title, allWarnings, "继续生成", "取消"))
                {
                    Debug.Log("[UIComponentCollectorInspector] 用户取消了代码生成。");
                    return;
                }
                Debug.LogWarning($"[UIComponentCollectorInspector] 代码生成时有以下警告：\n{string.Join("\n\n", warnings)}");
            }

            // ── 构建属性代码块 ──
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
                var bindings = UIComponentCollectorHelper.ParseNodeName(nodeName);
                if (bindings == null) continue;

                foreach (var binding in bindings)
                {
                    string expectedPropName = UIComponentCollectorHelper.GeneratePropertyName(binding.Prefix, binding.NodeName);
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