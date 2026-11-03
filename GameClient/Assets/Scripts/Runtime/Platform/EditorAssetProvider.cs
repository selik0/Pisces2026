#if UNITY_EDITOR
using System;
using GameEngine;
using UnityEditor;
using UnityEngine;

namespace GameClient
{
    /// <summary>
    /// 编辑器资源后端。AssetDatabase 本身是同步 API，这里只在 AssetManager.Tick 中报告完成，
    /// 使业务层仍然只能通过异步回调获得资源。
    /// </summary>
    public sealed class EditorAssetProvider : IAssetProvider
    {
        private sealed class Operation<T> : IAssetLoadOperation where T : UnityEngine.Object
        {
            private readonly string _path;
            private T _asset;
            private bool _started;

            public Operation(string path)
            {
                _path = path;
            }

            public bool IsDone
            {
                get
                {
                    if (!_started)
                    {
                        _started = true;
                        _asset = AssetDatabase.LoadAssetAtPath<T>(_path);
                    }

                    return true;
                }
            }

            public float Progress => _started ? 1f : 0f;

            public UnityEngine.Object Asset => _asset;

            public Exception Exception => null;
        }

        public IAssetLoadOperation LoadAsync<T>(string address) where T : UnityEngine.Object
        {
            return new Operation<T>(address);
        }

        public void Release(IAssetLoadOperation operation)
        {
        }
    }
}
#endif
