using System;
using UnityEditor;
using UnityEngine;

namespace GameEngineEditor
{
    /// <summary>
    /// 通用的编辑器 UI 工具方法。
    /// 封装常用的 EditorGUILayout / EditorGUI 绘制逻辑，提升 Inspector 面板代码的复用性。
    /// 所有方法仅使用 EditorGUILayout 和 EditorGUI 系列 API，避免依赖 IMGUIModule 的独立类型。
    /// </summary>
    public static class EditorUIHelper
    {
        // ==========================================================================
        //  标题与分隔
        // ==========================================================================

        /// <summary>
        /// 绘制一个带分隔线的加粗标题。
        /// </summary>
        public static void DrawSectionHeader(string title)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, Color.gray);
            EditorGUILayout.Space(2);
        }

        // ==========================================================================
        //  组件字段
        // ==========================================================================

        /// <summary>
        /// 绘制一个组件拖拽对象字段。
        /// </summary>
        public static Component DrawComponentField(string label, Component obj, bool allowSceneObjects = true)
        {
            return EditorGUILayout.ObjectField(label, obj, typeof(Component), allowSceneObjects) as Component;
        }

        /// <summary>
        /// 绘制一个带类型过滤的组件拖拽字段。
        /// </summary>
        public static Component DrawComponentField(string label, Component obj, Type componentType, bool allowSceneObjects = true)
        {
            return EditorGUILayout.ObjectField(label, obj, componentType, allowSceneObjects) as Component;
        }

        // ==========================================================================
        //  文本字段
        // ==========================================================================

        /// <summary>
        /// 绘制一个带标签的可编辑文本字段。
        /// </summary>
        public static string DrawTextField(string label, string value)
        {
            return EditorGUILayout.TextField(label, value);
        }

        /// <summary>
        /// 绘制一个多行文本编辑区域。
        /// </summary>
        public static string DrawTextArea(string label, string value)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            return EditorGUILayout.TextArea(value);
        }

        // ==========================================================================
        //  按钮
        // ==========================================================================

        /// <summary>
        /// 绘制一个居中按钮，返回是否被点击。
        /// </summary>
        public static bool DrawCenteredButton(string text)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool clicked = GUILayout.Button(text, GUILayout.Width(200), GUILayout.Height(30));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return clicked;
        }

        /// <summary>
        /// 绘制一个居中按钮（自定义尺寸）。
        /// </summary>
        public static bool DrawCenteredButton(string text, float width, float height)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            bool clicked = GUILayout.Button(text, GUILayout.Width(width), GUILayout.Height(height));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            return clicked;
        }

        /// <summary>
        /// 绘制一个普通按钮，返回是否被点击。
        /// </summary>
        public static bool DrawButton(string text)
        {
            return GUILayout.Button(text);
        }

        /// <summary>
        /// 绘制一个带颜色的按钮，返回是否被点击。
        /// </summary>
        public static bool DrawColoredButton(string text, Color color, float width, float height)
        {
            Color oldColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool clicked = GUILayout.Button(text, GUILayout.Width(width), GUILayout.Height(height));
            GUI.backgroundColor = oldColor;
            return clicked;
        }

        // ==========================================================================
        //  布局辅助
        // ==========================================================================

        /// <summary>灵活空间</summary>
        public static void FlexibleSpace()
        {
            GUILayout.FlexibleSpace();
        }

        // ==========================================================================
        //  列表绘制
        // ==========================================================================

        /// <summary>
        /// 绘制可折叠区域头部，返回是否展开。
        /// </summary>
        public static bool DrawFoldoutHeader(string title, bool expanded)
        {
            EditorGUILayout.BeginHorizontal();
            expanded = EditorGUILayout.Foldout(expanded, title, true, EditorStyles.foldoutHeader);
            EditorGUILayout.EndHorizontal();
            return expanded;
        }

        // ==========================================================================
        //  可序列化属性
        // ==========================================================================

        /// <summary>
        /// 绘制 SerializedProperty，支持子属性可见性。
        /// </summary>
        public static void DrawSerializedProperty(SerializedProperty property, bool includeChildren = false)
        {
            EditorGUILayout.PropertyField(property, includeChildren);
        }

        /// <summary>
        /// 获取 SerializedProperty 的数组元素。
        /// </summary>
        public static SerializedProperty GetArrayElement(SerializedProperty arrayProperty, int index)
        {
            return arrayProperty.GetArrayElementAtIndex(index);
        }

        // ==========================================================================
        //  帮助框
        // ==========================================================================

        /// <summary>
        /// 绘制信息提示框。
        /// </summary>
        public static void DrawHelpBox(string message, MessageType type = MessageType.Info)
        {
            EditorGUILayout.HelpBox(message, type);
        }

        // ==========================================================================
        //  路径选择
        // ==========================================================================

        /// <summary>
        /// 绘制一个带"浏览"按钮的路径选择字段。
        /// </summary>
        public static string DrawPathField(string label, string path, string title = "选择文件夹", string defaultName = "")
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("浏览", GUILayout.Width(50)))
            {
                string selectedPath = EditorUtility.OpenFolderPanel(title, path, defaultName);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    if (selectedPath.StartsWith(Application.dataPath))
                    {
                        selectedPath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                    }
                    path = selectedPath;
                }
            }
            EditorGUILayout.EndHorizontal();
            return path;
        }
    }
}