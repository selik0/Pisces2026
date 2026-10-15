using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameEngine
{
    public class UIRoot : MonoBehaviour
    {
        public Camera UICamera;
        public AudioListener UIAudioListener;
        public Canvas UICanvas;
        public CanvasScaler UICanvasScaler;
        public EventSystem UIEventSystem;
        public GraphicRaycaster UIGraphicRaycaster;
    }
}