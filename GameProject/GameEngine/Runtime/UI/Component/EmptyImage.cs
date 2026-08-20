using UnityEngine;
using UnityEngine.UI;

namespace GameEngine
{
    /// <summary>
    /// 空图形组件。不绘制任何内容，仅作为射线检测目标。
    /// 用于为透明区域或自定义热区提供可命中区域，避免依赖可见图片。
    /// </summary>
    [AddComponentMenu("UI/EmptyImage", 11)]
    public class EmptyImage : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }

        public override void SetMaterialDirty()
        {
        }

        public override void SetVerticesDirty()
        {
        }
    }
}
