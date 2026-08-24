using UnityEngine;

namespace GameEngine
{
    /// <summary>
    /// MonoBehaviour 单例基类。首次访问时查找场景实例，未找到则自动创建，并跨场景保留。
    /// </summary>
    /// <remarks>只能在 Unity 主线程访问 <see cref="Instance"/>。</remarks>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static bool _isQuitting;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                if (_isQuitting)
                {
                    Log.Error($"[MonoSingleton] Cannot access {typeof(T).FullName} while the application is quitting.");
                    return null;
                }

                _instance = FindObjectOfType<T>(true);
                if (_instance == null)
                {
                    var gameObject = new GameObject(typeof(T).Name);
                    _instance = gameObject.AddComponent<T>();
                }

                DontDestroyOnLoad(_instance.gameObject);
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Log.Warning($"[MonoSingleton] Duplicate {typeof(T).FullName} was destroyed.");
                Destroy(gameObject);
                return;
            }

            _instance = (T)this;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _isQuitting = true;
        }
    }
}
