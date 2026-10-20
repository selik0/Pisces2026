#if UNITY_EDITOR
using GameEngine;
using UnityEditor;
using UnityEngine;

namespace GameClient
{
    /// <summary>Unity 编辑器下使用 AssetDatabase 加载 UI 预制体。</summary>
    public sealed class EditorUIViewLoader : IUIViewLoader
    {
        public UIEntity Instantiate(string prefabPath, Transform parent)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PathUtility.GetUIPrefabPath(prefabPath));
            if (prefab == null)
            {
                Log.Error($"[EditorUIViewLoader] 未找到 UI 预制体: {prefabPath}");
                return null;
            }

            GameObject go = Object.Instantiate(prefab, parent);
            go.name = prefab.name;
            UIEntity entity = go.GetComponent<UIEntity>();
            if (entity == null)
            {
                Log.Warning($"[EditorUIViewLoader] 预制体 {prefabPath} 缺少 UIEntity 组件。");
                entity = go.AddComponent<UIEntity>();
            }
            
            return entity;
        }
    }
}
#endif
