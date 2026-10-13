using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameEngine;
using UnityEditor;
using UnityEngine;

namespace GameEngineEditor
{
    /// <summary>
    /// UIEntity 的自定义 Inspector。
    /// 展示完整的组件列表（可编辑 Name 和 Obj），提供"收集组件"和"生成代码"按钮。
    /// </summary>
    [CustomEditor(typeof(UIEntity))]
    public class UIEntityInspector : UnityEditor.Editor
    {
        // ======================================================================
        //  SerializedProperties
        // ======================================================================

        private SerializedProperty _componentListProp;

        // 代码生成模板
        private const string DefaultClassTemplate =
@"// 自动生成，请勿手动修改
// 生成时间：{GenTime}
using GameEngine;

namespace GameLogic
{
    public partial class {ClassName}
    {
{Properties}
    }
}
";

        private const string DefaultPropertyTemplate =
@"        public {TypeName} {PropertyName};";

        private const string LogicClassTemplate =
@"using GameEngine;

namespace GameLogic
{
    public partial class {ClassName} : UIView
    {
        // 设置为当前 UI 加载器使用的预制体地址。
        public override string PrefabPath => @""{PrefabPath}"";

        protected override void OnOpen()
        {
            base.OnOpen();
        }

{EventHandlers}
    }
}
";

        // ======================================================================
        //  生命周期
        // ======================================================================

        private void OnEnable()
        {
            _componentListProp = serializedObject.FindProperty(nameof(UIEntity.ComponentList));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            UIEntity collector = (UIEntity)target;

            // ── 标题 ──
            EditorUIHelper.DrawSectionHeader("UI 组件收集器");

            // ── 生成路径 ──
            EditorGUILayout.LabelField("属性绑定路径", "GameLogic/Runtime/UI/Generated");
            EditorGUILayout.LabelField("逻辑代码路径", "GameLogic/Runtime/UI/Logic");

            EditorGUILayout.Space(4);

            // ── 操作按钮 ──
            EditorGUILayout.BeginHorizontal();
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("收集组件", GUILayout.Height(30)))
            {
                var allRules = GetAllBindRulesFromAssetDatabase();
                UIEntityHelper.CollectComponents(collector, allRules);
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
                    {
                        nameCounts[name]++;
                    }
                    else
                    {
                        nameCounts[name] = 1;
                    }
                }
            }

            // ── 构建类型到规则的映射表 ──
            var allRules = GetAllBindRulesFromAssetDatabase();
            var typeToRule = new Dictionary<string, BindRule>();
            foreach (var rule in allRules)
            {
                Type type = UIEntityHelper.ResolveType(rule.ClassName);
                if (type != null && !typeToRule.ContainsKey(type.FullName))
                {
                    typeToRule[type.FullName] = rule;
                }
            }

            UIEntity collector = (UIEntity)target;

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
                {
                    selectedIndex = j + 1;
                }
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
            Dictionary<string, BindRule> typeToRule, UIEntity collector)
        {
            if (obj == null)
            {
                return;
            }

            string typeFullName = obj.GetType().FullName;
            if (!typeToRule.TryGetValue(typeFullName, out BindRule rule))
            {
                return;
            }

            string nodeName = obj.gameObject.name;
            var bindings = UIEntityHelper.ParseNodeName(nodeName);

            string propertyName;
            if (bindings != null && bindings.Count > 0)
            {
                // 从节点名解析结果中取第一个绑定的节点名部分
                propertyName = UIEntityHelper.GeneratePropertyName(rule.Prefix, bindings[0].NodeName);
            }
            else
            {
                // 节点名无下划线格式时直接使用完整节点名
                propertyName = UIEntityHelper.GeneratePropertyName(rule.Prefix, nodeName);
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
        private void GenerateCode(UIEntity collector)
        {
            string className = collector.name;
            using (var provider = new Microsoft.CSharp.CSharpCodeProvider())
            {
                if (!provider.IsValidIdentifier(className))
                {
                    EditorUtility.DisplayDialog("无法生成", "UI 根节点名称必须是合法的 C# 类型标识符。", "确定");
                    return;
                }
            }

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
                if (info == null || string.IsNullOrEmpty(info.Name))
                {
                    continue;
                }

                if (nameCounts.ContainsKey(info.Name))
                {
                    nameCounts[info.Name]++;
                }
                else
                {
                    nameCounts[info.Name] = 1;
                }
            }

            var duplicates = new List<string>();
            foreach (var kvp in nameCounts)
            {
                if (kvp.Value > 1)
                {
                    duplicates.Add($"  · {kvp.Key}（出现 {kvp.Value} 次）");
                }
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
                {
                    msg += $"\n  · {nullObjCount} 个条目对组件引用为 null（Obj 为空）";
                }

                if (nullInfoCount > 0)
                {
                    msg += $"\n  · {nullInfoCount} 个条目为 null（缺失条目）";
                }

                msg += "\n\n丢失引用的条目将被跳过，不会生成对应属性。";
                warnings.Add(msg);
            }

            // 重复属性会生成无法编译的代码，必须先修正。
            if (duplicates.Count > 0)
            {
                EditorUtility.DisplayDialog("无法生成", string.Join("\n", duplicates), "确定");
                return;
            }

            // 有警告则弹窗确认
            if (warnings.Count > 0)
            {
                string title = "生成代码 - 警告";
                string allWarnings = string.Join("\n\n", warnings);
                allWarnings += "\n\n是否继续生成代码？";

                if (!EditorUtility.DisplayDialog(title, allWarnings, "继续生成", "取消"))
                {
                    Debug.Log("[UIEntityInspector] 用户取消了代码生成。");
                    return;
                }
                Debug.LogWarning($"[UIEntityInspector] 代码生成时有以下警告：\n{string.Join("\n\n", warnings)}");
            }

            // ── 构建属性代码块 ──
            StringBuilder propertiesBuilder = new StringBuilder();
            StringBuilder assignments = new StringBuilder();
            StringBuilder clearAssignments = new StringBuilder();
            StringBuilder bindEvents = new StringBuilder();
            StringBuilder unbindEvents = new StringBuilder();
            StringBuilder eventMethods = new StringBuilder();

            for (int i = 0; i < collector.ComponentList.Count; i++)
            {
                ComponentInfo info = collector.ComponentList[i];
                if (info == null || info.Obj == null)
                {
                    continue;
                }

                string propertyName = info.Name;
                string typeName = GetTypeNameForComponent(info.Obj);
                using (var provider = new Microsoft.CSharp.CSharpCodeProvider())
                {
                    if (!provider.IsValidIdentifier(propertyName))
                    {
                        EditorUtility.DisplayDialog("无法生成", $"组件属性名不是合法标识符：{propertyName}", "确定");
                        return;
                    }
                }

                // 添加属性声明
                propertiesBuilder.AppendLine(DefaultPropertyTemplate
                    .Replace("{TypeName}", typeName)
                    .Replace("{PropertyName}", propertyName));

                assignments.AppendLine($"            {propertyName} = Entity.GetCollectedComponent<{typeName}>(\"{propertyName}\");");
                clearAssignments.AppendLine($"            {propertyName} = null;");
                BindRule rule = FindRuleForProperty(collector, info);
                if (rule != null && !string.IsNullOrWhiteSpace(rule.TemplateCode1))
                {
                    string eventName = GetEventName(propertyName, rule.Prefix);
                    bindEvents.AppendLine(ReplaceTemplate(rule.TemplateCode1, propertyName, typeName, className, eventName));
                    eventMethods.AppendLine(ReplaceTemplate(rule.TemplateCode2, propertyName, typeName, className, eventName));
                    unbindEvents.AppendLine(ReplaceTemplate(rule.TemplateCode3, propertyName, typeName, className, eventName));
                }
                else
                {
                    AppendComponentEvents(info, bindEvents, unbindEvents, eventMethods);
                }
            }

            propertiesBuilder.AppendLine($@"
        protected override void OnBind()
        {{
            base.OnBind();
{assignments}
{bindEvents}        }}

        protected override void OnUnbind()
        {{
{unbindEvents}
{clearAssignments}            base.OnUnbind();
        }}

{eventMethods}");

            // 生成完整类代码
            string classCode = DefaultClassTemplate
                .Replace("{ClassName}", className)
                .Replace("{GenTime}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("{Properties}", propertiesBuilder.ToString());

            // 写入文件
            string runtimeRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../../GameProject/GameLogic/Runtime"));
            if (!Directory.Exists(runtimeRoot))
            {
                EditorUtility.DisplayDialog("无法生成", $"找不到业务源码根目录：{runtimeRoot}", "确定");
                return;
            }

            string generatedFolder = Path.Combine(runtimeRoot, "UI/Generated");
            string logicFolder = Path.Combine(runtimeRoot, "UI/Logic");
            string filePath = Path.Combine(generatedFolder, $"{className}_Bind.cs");
            string logicPath = Path.Combine(logicFolder, $"{className}.cs");
            if (File.Exists(filePath) && !File.ReadAllText(filePath).StartsWith("// 自动生成，请勿手动修改", StringComparison.Ordinal))
            {
                EditorUtility.DisplayDialog("无法生成", $"目标文件不包含自动生成标记，请先检查手写内容：{filePath}", "确定");
                return;
            }

            Directory.CreateDirectory(generatedFolder);
            Directory.CreateDirectory(logicFolder);
            File.WriteAllText(filePath, classCode, Encoding.UTF8);
            bool logicExists = File.Exists(logicPath);
            if (!logicExists)
            {
                string prefabPath = AssetDatabase.GetAssetPath(collector.gameObject);
                if (string.IsNullOrEmpty(prefabPath))
                {
                    prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(collector.gameObject);
                }

                const string uiPrefabSegment = "UIPrefab/";
                int uiPrefabIndex = prefabPath.IndexOf("/" + uiPrefabSegment, StringComparison.Ordinal);
                if (uiPrefabIndex >= 0)
                {
                    prefabPath = prefabPath.Substring(uiPrefabIndex + 1);
                }

                string logicCode = LogicClassTemplate.Replace("{ClassName}", className)
                    .Replace("{PrefabPath}", prefabPath.Replace("\"", "\"\""));
                var handlers = new StringBuilder();
                foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
                    eventMethods.ToString(), @"        partial void ([^;]+);"))
                {
                    handlers.AppendLine($"        partial void {match.Groups[1].Value}\n        {{\n        }}\n");
                }

                logicCode = logicCode.Replace("{EventHandlers}", handlers.ToString());
                File.WriteAllText(logicPath, logicCode, Encoding.UTF8);
            }

            string message = $"属性与事件绑定代码：{filePath}\n\n逻辑代码：{logicPath}\n{(logicExists ? "已保留现有逻辑文件。" : "已创建逻辑文件。请确认 PrefabPath 与加载器匹配。 ")}\n\n请编译 GameProject 后在 Unity 中使用。";
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("生成完成", message, "确定");
            Log.Debug($"[UIEntityInspector] 代码已生成: {filePath}");
        }

        private static void AppendComponentEvents(ComponentInfo info, StringBuilder bindEvents,
            StringBuilder unbindEvents, StringBuilder eventMethods)
        {
            if (info.Obj is UnityEngine.UI.Button)
            {
                AppendEvent(info.Name, "onClick", "Click", "", bindEvents, unbindEvents, eventMethods);
            }
            else if (info.Obj is UnityEngine.UI.Toggle)
            {
                AppendEvent(info.Name, "onValueChanged", "ValueChanged", "bool", bindEvents, unbindEvents, eventMethods);
            }
            else if (info.Obj is UnityEngine.UI.Slider || info.Obj is UnityEngine.UI.Scrollbar)
            {
                AppendEvent(info.Name, "onValueChanged", "ValueChanged", "float", bindEvents, unbindEvents, eventMethods);
            }
            else if (info.Obj is UnityEngine.UI.InputField || info.Obj is TMPro.TMP_InputField)
            {
                AppendEvent(info.Name, "onValueChanged", "ValueChanged", "string", bindEvents, unbindEvents, eventMethods);
                AppendEvent(info.Name, "onEndEdit", "EndEdit", "string", bindEvents, unbindEvents, eventMethods);
            }
            else if (info.Obj is UnityEngine.UI.Dropdown || info.Obj is TMPro.TMP_Dropdown)
            {
                AppendEvent(info.Name, "onValueChanged", "ValueChanged", "int", bindEvents, unbindEvents, eventMethods);
            }
            else if (info.Obj is UnityEngine.UI.ScrollRect)
            {
                AppendEvent(info.Name, "onValueChanged", "ValueChanged", "UnityEngine.Vector2", bindEvents, unbindEvents, eventMethods);
            }
        }

        private static void AppendEvent(string propertyName, string eventName, string suffix, string parameterType,
            StringBuilder bindEvents, StringBuilder unbindEvents, StringBuilder eventMethods)
        {
            string prefix = "";
            foreach (BindRule rule in GetAllBindRulesFromAssetDatabase())
            {
                if (propertyName.StartsWith(rule.Prefix, StringComparison.Ordinal) && rule.Prefix.Length > prefix.Length)
                {
                    prefix = rule.Prefix;
                }
            }

            string methodName = $"On{GetEventName(propertyName, prefix)}{suffix}";
            string parameter = string.IsNullOrEmpty(parameterType) ? "" : parameterType + " value";
            bindEvents.AppendLine($"            {propertyName}.{eventName}.AddListener({methodName});");
            unbindEvents.AppendLine($"            {propertyName}.{eventName}.RemoveListener({methodName});");
            eventMethods.AppendLine($"        partial void {methodName}({parameter});");
        }

        private static string GetEventName(string propertyName, string prefix)
        {
            string name = propertyName.StartsWith(prefix, StringComparison.Ordinal)
                ? propertyName.Substring(prefix.Length) : propertyName;
            if (name.Length == 0)
            {
                name = propertyName;
            }

            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        /// <summary>
        /// 获取组件对应的类型全名。
        /// </summary>
        private string GetTypeNameForComponent(Component comp)
        {
            if (comp == null)
            {
                return "Component";
            }

            return comp.GetType().FullName;
        }

        /// <summary>
        /// 从项目中通过 AssetDatabase 加载所有 ComponentBindConfig，合并返回所有绑定规则。
        /// </summary>
        private static List<BindRule> GetAllBindRulesFromAssetDatabase()
        {
            var result = new List<BindRule>();

            string[] guids = AssetDatabase.FindAssets("t:ComponentBindConfig");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var config = AssetDatabase.LoadAssetAtPath<ComponentBindConfig>(path);
                if (config == null || config.Rules == null)
                {
                    continue;
                }

                foreach (var rule in config.Rules)
                {
                    if (rule != null && !string.IsNullOrEmpty(rule.Prefix) && !string.IsNullOrEmpty(rule.ClassName))
                    {
                        result.Add(rule);
                    }
                }
            }

            return result.OrderByDescending(rule => rule.Priority).ToList();
        }

        /// <summary>
        /// 根据 ComponentInfo 的属性名反查绑定的规则。
        /// </summary>
        private BindRule FindRuleForProperty(UIEntity collector, ComponentInfo info)
        {
            var allRules = GetAllBindRulesFromAssetDatabase();
            if (allRules.Count == 0)
            {
                return null;
            }

            // 尝试通过节点解析反查：遍历所有子节点找到匹配 component 来定位对应的前缀
            var allTransforms = collector.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                if (t == collector.transform || t != info.Obj.transform)
                {
                    continue;
                }

                string nodeName = t.name;
                var bindings = UIEntityHelper.ParseNodeName(nodeName);
                if (bindings == null)
                {
                    continue;
                }

                foreach (var binding in bindings)
                {
                    string expectedPropName = UIEntityHelper.GeneratePropertyName(binding.Prefix, binding.NodeName);
                    if (expectedPropName == info.Name)
                    {
                        // 在所有规则中查找匹配的 Prefix
                        foreach (var rule in allRules)
                        {
                            Type type = UIEntityHelper.ResolveType(rule.ClassName);
                            if (rule.Prefix == binding.Prefix && type != null && type.IsInstanceOfType(info.Obj))
                            {
                                return rule;
                            }
                        }
                    }
                }
            }

            foreach (BindRule rule in allRules)
            {
                Type type = UIEntityHelper.ResolveType(rule.ClassName);
                if (type != null && type.IsInstanceOfType(info.Obj) &&
                    info.Name.StartsWith(rule.Prefix, StringComparison.Ordinal))
                {
                    return rule;
                }
            }

            return null;
        }

        /// <summary>
        /// 替换模板中的占位符。
        /// 占位符约定：
        ///   {PropertyName} → 属性名
        ///   {EventName} → 去掉组件前缀并将首字母大写后的名称
        ///   {TypeName}      → 类型名
        ///   {GameObjectName} → 所属 GameObject 名称
        /// </summary>
        private string ReplaceTemplate(string template, string propertyName, string typeName, string gameObjectName, string eventName)
        {
            if (string.IsNullOrEmpty(template))
            {
                return template;
            }

            return template
                .Replace("{EventName}", eventName)
                .Replace("{PropertyName}", propertyName)
                .Replace("{TypeName}", typeName)
                .Replace("{GameObjectName}", gameObjectName);
        }

        // ======================================================================
        //  清理
        // ======================================================================

        /// <summary>
        /// 清理无效的组件引用（Obj 为 null 的条目）。
        /// </summary>
        private void CleanInvalidReferences(UIEntity collector)
        {
            int removed = collector.ComponentList.RemoveAll(item => item == null || item.Obj == null);
            if (removed > 0)
            {
                EditorUtility.SetDirty(collector);
                Debug.Log($"[UIEntityInspector] 已清理 {removed} 条无效引用。");
            }
            else
            {
                Debug.Log("[UIEntityInspector] 没有无效引用需要清理。");
            }
        }
    }
}
