using UnityEngine;
using System.Collections;
using GenvidSDKCSharp;
using System;

public class GenvidSessionManager : MonoBehaviour
{
    private enum State
    {
        Uninitialized,
        Initializing,
        Initialized,
        Destroying,
        Destroyed
    }

    private bool m_IsCreated = false;

    // Properties
    public bool ActivateSDK = true;
    public bool AutoInitialize = true;
    public GenvidSession Session;
    public bool ActivateDebugLog = false;

    // Static variables
    private static GenvidSessionManager m_Instance;
    private static object _lock = new object();
    private static State m_State;

    // Static getter accessor
    public static bool IsDestroyed { get { return m_State == State.Destroyed; } }
    public static bool IsDestroying { get { return m_State == State.Destroying; } }
    public static bool IsInitialized { get { return m_State == State.Initialized; } }

    public static GenvidSessionManager Instance
    {
        get
        {
            if (IsDestroyed)
            {
                /*Debug.LogWarning("[Singleton] Instance '" + typeof(GenvidSessionManager) +
                                    "' already destroyed on application quit." +
                                    " Won't create again - returning null.");*/
                return null;
            }

            lock (_lock)
            {
                if (m_Instance == null)
                {
                    var instances = FindObjectsOfType<GenvidSessionManager>();
                    m_Instance = FindObjectOfType<GenvidSessionManager>();

                    if (instances.Length > 1)
                    {
                        Debug.LogError("[Singleton] Something went really wrong " +
                                        " - there should never be more than 1 singleton!" +
                                        " Reopening the scene might fix it.");
                        return m_Instance;
                    }

                    if (m_Instance == null)
                    {
                        GameObject singleton = new GameObject();
                        m_Instance = singleton.AddComponent<GenvidSessionManager>();
                        singleton.name = "(singleton) " + typeof(GenvidSessionManager).ToString();


                        DontDestroyOnLoad(singleton);

                        Debug.Log("[Singleton] An instance of " + typeof(GenvidSessionManager) +
                                    " is needed in the scene, so '" + singleton +
                                    "' was created with DontDestroyOnLoad.");
                    }
                    else
                    {
                        DontDestroyOnLoad(m_Instance.gameObject);
                        Debug.Log("[Singleton] Using instance already created: " + m_Instance.gameObject.name);
                    }
                }

                return m_Instance;
            }
        }
    }

    public void Initialize()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++) 
        {
            if (args[i] == "-Genvid") 
            {
                ActivateSDK = true;
            }
        }

        if (ActivateSDK && !IsInitialized)
        {
            // 32 bits -> x86
            // 64 bits -> x86_64

            string dllPath = "/Plugins";
            string editorDllPath = "/Genvid/SDK/Plugins";
            string[] paths = { "", "/x86" };
            uint pathIndex = 0;
            bool arch86_64 = (IntPtr.Size == 8);

            while (true)
            {
                if (Application.isEditor)
                {
                    dllPath = editorDllPath + (arch86_64 ? "/x64" : "/x86");
                }
                else if (paths[pathIndex].Length != 0)
                {
                    dllPath += arch86_64 ? (paths[pathIndex] + "_64") : paths[pathIndex];
                }

                if (!GenvidSDK.LoadGenvidDll(Application.dataPath + dllPath))
                {
                    if (pathIndex == paths.Length - 1)
                    {
                        Debug.LogError("Failed to load genvid.dll from " + Application.dataPath + dllPath);
                        return;
                    }
                    else
                    {
                        ++pathIndex;
                        continue;
                    }
                }
                else
                {
                    Debug.Log("genvid.dll successfully loaded from " + Application.dataPath + dllPath);
                    break;
                }
            }

            GenvidSDK.Status gvStatus = GenvidSDK.Initialize();
            if (GenvidSDK.StatusFailed(gvStatus))
            {
                m_State = State.Uninitialized;
                Debug.LogError("Error while Genvid Initialize : " + GenvidSDK.StatusToString(gvStatus));
            }
            else
            {
                m_State = State.Initialized;
                if (ActivateDebugLog)
                {
                    Debug.Log("Genvid Initialize performed correctly.");
                }
            }

            //In case of User manually doing initialize
            if (!m_IsCreated)
            {
                OnEnable();
            }
        }
    #endif
    }

    public void Terminate()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
		// SafeApplicationQuit() is setting m_State to Destroying.
        if (ActivateSDK && (IsInitialized || IsDestroying))
        {
            var gvStatus = GenvidSDK.Terminate();
            if (GenvidSDK.StatusFailed(gvStatus))
            {
                Debug.LogError("Error while doing the terminate process : " + GenvidSDK.StatusToString(gvStatus));
            }
            else
            {
                m_State = State.Uninitialized;
                if (ActivateDebugLog)
                {
                    Debug.Log("Genvid Terminate performed correctly.");
                }
            }

            GenvidSDK.UnloadGenvidDll();
        }
    #endif
    }

    private IEnumerator SafeApplicationQuit()
    {
        m_State = State.Destroying;

        OnDisable();

        if (ActivateSDK && Session != null)
        {
            DestroyImmediate(Session);
        }

        Terminate();
        yield return null;
    }

    private void Awake()
    {
        m_State = State.Initializing;

        //Need to keep instance alive before switching scene - mandatory when Genvid SDK is not active
        var instanceInit = Instance;

        if (AutoInitialize)
        {
            Initialize();
        }
    }

    void FixedUpdate()
    {
        if (ActivateSDK && m_IsCreated && IsInitialized)
        {
            var gvStatus = GenvidSDK.CheckForEvents();
            if ((GenvidSDK.StatusFailed(gvStatus)) && gvStatus != GenvidSDK.Status.ConnectionTimeout)
            {
                Debug.LogError("Error while doing the CheckForEvents : " + GenvidSDK.StatusToString(gvStatus));
            }
            else if (ActivateDebugLog)
            {
                Debug.Log("Genvid CheckForEvents performed correctly.");
            }
        }
    }

    void OnEnable()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if(Session != null && !m_IsCreated && ActivateSDK && IsInitialized)
        {
            Session.Create();
            m_IsCreated = true;
        }
    #endif
    }

    void OnDisable()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (Session != null && m_IsCreated && ActivateSDK && (IsInitialized || IsDestroying))
        {
            Session.Destroy();
            m_IsCreated = false;
        }
    #endif
    }

    void OnApplicationQuit()
    {
        if(ActivateSDK && IsInitialized)
        {
            StartCoroutine(SafeApplicationQuit());
            m_State = State.Destroyed;
        }
    }
}
