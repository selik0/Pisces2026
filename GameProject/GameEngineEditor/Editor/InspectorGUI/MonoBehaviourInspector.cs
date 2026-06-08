using UnityEditor;
using UnityEngine;

namespace GameEngineEditor
{
    /// <summary>
    /// GameEngine MonoBehaviour 基础自定义 Inspector 示例。
    /// 继承此类为具体的 MonoBehaviour 编写自定义面板。
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    public class MonoBehaviourInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // 绘制默认 Inspector 内容
            DrawDefaultInspector();
        }
    }
}
