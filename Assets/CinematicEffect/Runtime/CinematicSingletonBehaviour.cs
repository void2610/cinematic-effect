using UnityEngine;

namespace Void2610.CinematicEffect
{
    /// <summary>
    /// CinematicEffect 内部で使うシングルトン MonoBehaviour 基底。
    /// インスタンスが存在しない場合は自動作成し、重複を防ぐ。
    /// </summary>
    public class CinematicSingletonBehaviour<T> : MonoBehaviour where T : Component
    {
        public static T Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;

                _instance = FindFirstObjectByType<T>();
                if (_instance != null)
                    return _instance;

                // インスタンスが存在しない場合は自動作成
                var singletonObject = new GameObject(typeof(T).Name);
                _instance = singletonObject.AddComponent<T>();

                return _instance;
            }
        }

        /// <summary>
        /// インスタンスが存在するかどうかを返す（作成はしない）
        /// </summary>
        public static bool HasInstance => _instance != null;
        private static T _instance;

        protected bool IsDontDestroyOnLoad = true;

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                // 子オブジェクトには DDOL が効かないので警告を避ける
                if (IsDontDestroyOnLoad && transform.parent == null) DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
