using UnityEngine;
using UnityEngine.UI;

namespace GameEngine
{
    /// <summary>
    /// 增强版图片组件。在 <see cref="Image"/> 基础上支持左右镜像显示。
    /// </summary>
    [AddComponentMenu("UI/ImagePro", 12)]
    public class ImagePro : Image
    {
        [SerializeField]
        private bool _horizontalFlip;

        /// <summary>是否左右镜像显示。</summary>
        public bool HorizontalFlip
        {
            get => _horizontalFlip;
            set
            {
                if (_horizontalFlip == value)
                {
                    return;
                }

                _horizontalFlip = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);
            ApplyHorizontalFlip(toFill);
        }

        void ApplyHorizontalFlip(VertexHelper toFill)
        {
            if (!_horizontalFlip || overrideSprite ==null || type != Type.Simple)
            {
                return;
            }
            for (int i = 0; i < toFill.currentVertCount; i++)
            {
                UIVertex vertex = new UIVertex();
                toFill.PopulateUIVertex(ref vertex, i);
                vertex.uv0.x = 1f - vertex.uv0.x;
                toFill.SetUIVertex(vertex, i);
            }
        }
    }
}
