using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// UI 预制体加载器。
    /// 框架默认使用 <see cref="DefaultUIViewLoader"/>（基于 Resources），
    /// 游戏可替换为 Addressables / AssetBundle 等实现。
    /// </summary>
    public interface IUIViewLoader
    {
        /// <summary>实例化指定路径的 UI 预制体并返回其 UIEntity。</summary>
        /// <param name="prefabPath">预制体路径。</param>
        /// <param name="parent">挂载父节点。</param>
        /// <returns>预制体根节点上的 UIEntity，失败返回 null。</returns>
        UIEntity Instantiate(string prefabPath, Transform parent);
    }
}
