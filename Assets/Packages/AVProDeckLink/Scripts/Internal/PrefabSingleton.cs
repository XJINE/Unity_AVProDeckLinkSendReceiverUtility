using UnityEngine;

//-----------------------------------------------------------------------------
// Copyright 2014-2017 RenderHeads Ltd.  All rights reserved.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProDeckLink
{
    public class PrefabSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;

        private static object _lock = new object();

        public static T Instance
        {
            get
            {
                if (applicationIsQuitting)
                {
                    return null;
                }

                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = (T)FindObjectOfType(typeof(T));

                        if (FindObjectsOfType(typeof(T)).Length > 1)
                        {
                            Debug.LogError("[Prefab Singleton] Something went really wrong " +
                                " - there should never be more than 1 singleton!" +
                                " Reopenning the scene might fix it.");
                            return _instance;
                        }

                        if (_instance == null)
                        {
                            T[] singleton = (T[])Resources.FindObjectsOfTypeAll(typeof(T));

                            if(singleton.Length > 1)
                            {
                                Debug.LogError("[Prefab Singleton] There should be only one instance of a prefab singleton " + singleton.Length + " found instead");
                                return null;
                            }
                            else if(singleton.Length == 0)
                            {
                                Resources.Load("DeviceExplorerManager(Singleton)");

								singleton = (T[])Resources.FindObjectsOfTypeAll(typeof(T));

                                if (singleton.Length != 1)
                                {
                                    Debug.LogError("[Prefab Singleton] There should be only one instance of a prefab singleton " + singleton.Length + " found instead");
                                    return null;
                                }
                            }

                            _instance = (T)Instantiate(singleton[0]);

                            DontDestroyOnLoad(_instance);
                        }
                    }

                    return _instance;
                }
            }
        }

        private static bool applicationIsQuitting = false;

        public void OnDestroy()
        {
            applicationIsQuitting = true;
        }
    }
}
