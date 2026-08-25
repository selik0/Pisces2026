using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// 默认 UI 预制体加载器，基于 <see cref="Resources.Load{T}"/> 同步加载。
    /// </summary>
    public sealed class DefaultUIViewLoader : IUIViewLoader
    {
        /// <inheritdoc />
        public UIEntity Instantiate(string prefabPath, Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null)
            {
                Log.Error($"[UIViewLoader] 未找到 UI 预制体: {prefabPath}");
                return null;
            }

            GameObject go = UnityEngine.Object.Instantiate(prefab, parent);
            go.name = prefab.name;

            UIEntity entity = go.GetComponent<UIEntity>();
            if (entity == null)
            {
                Log.Warning($"[UIViewLoader] 预制体 {prefabPath} 缺少 UIEntity 组件，已自动添加。");
                entity = go.AddComponent<UIEntity>();
            }

            return entity;
        }
    }
}
