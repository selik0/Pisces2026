using TMPro.EditorUtilities;
using UnityEditor;

namespace GameEngineEditor
{
    /// <summary>
    /// 在 TextMeshPro SDF 标准材质面板后追加灰度强度属性。
    /// </summary>
    public class TMPGrayscaleShaderGUI : TMP_SDFShaderGUI
    {
        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);

            MaterialProperty grayscaleProperty = FindProperty("_Grayscale", properties, false);
            if (grayscaleProperty == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("扩展效果", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(grayscaleProperty, "置灰强度");
        }
    }
}
