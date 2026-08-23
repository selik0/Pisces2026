using GameEngine;
using UnityEditor;
using UnityEngine;

namespace GameEngineEditor
{
    /// <summary>
    /// FontLanguageConfig 自定义 Inspector：显示字体属性，隐藏 SourceFontName/TargetFontPath，
    /// 两个隐藏字段在对应字体赋值或修改时自动填充。
    /// </summary>
    [CustomEditor(typeof(FontLanguageConfig))]
    public class FontLanguageConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty _entries;

        private void OnEnable()
        {
            _entries = serializedObject.FindProperty(nameof(FontLanguageConfig.Entries));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_entries.FindPropertyRelative("Array.size"), new GUIContent("条目数量"));

            for (int i = 0; i < _entries.arraySize; i++)
            {
                SerializedProperty element = _entries.GetArrayElementAtIndex(i);
                EditorGUILayout.LabelField($"条目 {i}", EditorStyles.boldLabel);

                EditorGUILayout.PropertyField(element.FindPropertyRelative(nameof(FontLanguageEntry.LanguageKey)));
#if GAME_DEBUG
                DrawFontProperty(element, nameof(FontLanguageEntry.SourceFont), nameof(FontLanguageEntry.SourceFontName),
                    font => font != null ? font.name : string.Empty);
                DrawFontProperty(element, nameof(FontLanguageEntry.TargetFont), nameof(FontLanguageEntry.TargetFontPath),
                    font => AssetDatabase.GetAssetPath(font));
#endif
                EditorGUILayout.PropertyField(element.FindPropertyRelative(nameof(FontLanguageEntry.SizeScale)));
                EditorGUILayout.PropertyField(element.FindPropertyRelative(nameof(FontLanguageEntry.LineHeightRatio)));
            }

            serializedObject.ApplyModifiedProperties();
        }

#if GAME_DEBUG
        /// <summary>绘制字体属性，字体赋值或修改时自动填充对应隐藏字段。</summary>
        private static void DrawFontProperty(SerializedProperty element, string fontField, string fillField, System.Func<Object, string> fillValue)
        {
            SerializedProperty fontProp = element.FindPropertyRelative(fontField);
            SerializedProperty fillProp = element.FindPropertyRelative(fillField);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(fontProp);
            if (EditorGUI.EndChangeCheck())
            {
                fillProp.stringValue = fillValue(fontProp.objectReferenceValue);
            }
        }
#endif
    }
}
