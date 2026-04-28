using UnityEngine;

namespace EFrameWork.Runtime.Base
{
    public class SingletonMono<T> : MonoBehaviour where T : SingletonMono<T>
    {
        private static T m_instance;

        public static T Instance
        {
            get
            {
                if (m_instance == null) return Init();
                return m_instance;
            }
        }

        public static T Init()
        {
            if (m_instance == null)
            {
                m_instance = FindFirstObjectByType<T>();
                if (m_instance == null)
                {
                    GameObject singletonObject = new(typeof(T).Name);
                    if (Application.isPlaying) DontDestroyOnLoad(singletonObject);
                    m_instance = singletonObject.AddComponent<T>();
                }
            }

            return m_instance;
        }
    }
}
