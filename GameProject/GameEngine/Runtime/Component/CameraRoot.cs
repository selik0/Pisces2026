using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameEngine
{
    /// <summary>集中保存启动场景中的主相机和 UI 相机引用。</summary>
    public sealed class CameraRoot : MonoBehaviour
    {
        public Camera MainCamera;
        public UniversalAdditionalCameraData MainCameraData;
        public Camera UICamera;
        public UniversalAdditionalCameraData UICameraData; 
    }
}
