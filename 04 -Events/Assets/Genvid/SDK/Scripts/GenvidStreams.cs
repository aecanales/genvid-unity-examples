using GenvidSDKCSharp;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Text;
using System.Collections;

public class GenvidStreams : GenvidStreamBase
{
    [Serializable]
    public class StreamEvent : UnityEvent<string>
    {
    }

    [Serializable]
    public class GenvidStreamElement
    {
        [Tooltip("Stream Name")]
        public string Id;

        [Range(0.001f, 60.0f)]
        [Tooltip("Stream framerate")]
        public float Framerate = 30f;

        [Tooltip("Start Callback")]
        public StreamEvent OnStart;

        [Tooltip("Stream Callback")]
        public StreamEvent OnSubmitStream;
        public float Framecount { get; set; }

        private bool OnStartSubmitted = false;

        public bool GetOnStartSubmitted() { return OnStartSubmitted; }
        public void SetOnStartSubmitted(bool Submitted) { OnStartSubmitted = Submitted; }
        public float SecondsSinceLastSubmit { get; set; }
    }

    public GenvidStreamElement[] Ids;
    private bool m_IsCreated = false;
    
    public new bool Create()
    {
        bool result = true;

    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (GenvidSessionManager.Instance.ActivateSDK && !m_IsCreated && GenvidSessionManager.IsInitialized)
        {
            foreach (var stream in Ids)
            {
                var status = GenvidSDK.CreateStream(stream.Id);
                if (GenvidSDK.StatusFailed(status))
                {
                    result = false;
                    Debug.LogError("Error while creating the " + stream.Id + " stream: " + GenvidSDK.StatusToString(status));
                }
                else
                {
                    SetFrameRate(stream.Id, stream.Framerate);
                    if (GenvidSessionManager.Instance.ActivateDebugLog)
                    {
                        Debug.Log("Genvid Create data stream named " + stream.Id + " performed correctly.");
                    }
                }
            }
            m_IsCreated = true;
        }
    #endif

        return result;
    }

    public new void Destroy()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (GenvidSessionManager.Instance.ActivateSDK && m_IsCreated)
        {
            foreach (var stream in Ids)
            {
                var status = GenvidSDK.DestroyStream(stream.Id);
                if (GenvidSDK.StatusFailed(status))
                {
                    Debug.LogError("Error while destroying the " + stream + " stream: " + GenvidSDK.StatusToString(status));
                }
                else if (GenvidSessionManager.Instance.ActivateDebugLog)
                {
                    Debug.Log("Genvid Destroy data stream named " + stream.Id + " performed correctly.");
                }
            }
            m_IsCreated = false;
        }
    #endif
    }

    private void Update()
    {
        if (m_IsCreated)
        {
            foreach (var stream in Ids)
            {
                if (stream.OnSubmitStream != null)
                {
                    stream.OnSubmitStream.Invoke(stream.Id);
                }
                if (stream.OnStart != null && !stream.GetOnStartSubmitted())
                {
                    stream.OnStart.Invoke(stream.Id);
                    stream.SetOnStartSubmitted(true);
                }
            }
        }
    }

    public bool SubmitGameData(object streamID, ref byte[] data, int size)
    {
        if (!m_IsCreated)
        {
            if (GenvidSessionManager.Instance.ActivateSDK && !GenvidSessionManager.IsInitialized)
            {
                Debug.LogError("Genvid SDK is not initialized: unable to submit game data.");
            }
            else
            {
                Debug.LogError(String.Format("Unable to submit game data on inexistant stream '{0}'.", streamID));
            }

            return false;
        }

        var status = GenvidSDK.SubmitGameData(GenvidSDK.GetCurrentTimecode(), streamID.ToString(), data, size);

        if (GenvidSDK.StatusFailed(status))
        {
            Debug.LogError(String.Format("`SubmitGameData` failed with error: {0}", GenvidSDK.StatusToString(status)));
            return false;
        }

        if (GenvidSessionManager.Instance.ActivateDebugLog)
        {
            Debug.Log(String.Format("Genvid correctly submitted game data: {0}", data));
        }

        return true;
    }

    public bool SubmitAnnotation(object streamID, ref byte[] data, int size)
    {
        if (!m_IsCreated)
        {
            if (GenvidSessionManager.Instance.ActivateSDK && !GenvidSessionManager.IsInitialized)
            {
                Debug.LogError("Genvid SDK is not initialized: unable to submit annotation.");
            }
            else
            {
                Debug.LogError(String.Format("Unable to submit annotation on inexistant stream '{0}'.", streamID));
            }

            return false;
        }

        var status = GenvidSDK.SubmitAnnotation(GenvidSDK.GetCurrentTimecode(), streamID.ToString(), data, size);

        if (GenvidSDK.StatusFailed(status))
        {
            Debug.LogError(String.Format("`SubmitAnnotation` failed with error: {0}", GenvidSDK.StatusToString(status)));
            return false;
        }

        if (GenvidSessionManager.Instance.ActivateDebugLog)
        {
            Debug.Log(String.Format("Genvid correctly submitted annotation: {0}", data));
        }

        return true;
    }

    public bool SubmitGameData(object streamID, ref byte[] data)
    {
        return SubmitGameData(streamID, ref data, data.Length);
    }

    public bool SubmitAnnotation(object streamID, ref byte[] data)
    {
        return SubmitAnnotation(streamID, ref data, data.Length);
    }

    public bool SubmitGameData(object streamID, string data)
    {
        if (data == null)
        {
            Debug.LogError("Unable to handle `null` data. Submitting game data failed.");
            return false;
        }

        var dataAsBytes = Encoding.Default.GetBytes(data);
        return SubmitGameData(streamID, ref dataAsBytes);
    }

    public bool SubmitAnnotation(object streamID, string data)
    {
        if (data == null)
        {
            Debug.LogError("Unable to handle `null` data. Submitting annotation failed.");
            return false;
        }

        var dataAsBytes = Encoding.Default.GetBytes(data);
        return SubmitAnnotation(streamID, ref dataAsBytes);
    }

    public bool SubmitGameDataJSON(object streamID, object data)
    {
        if (data == null)
        {
            Debug.LogError("Unable to handle `null` data. Submitting game data failed.");
            return false;
        }

        var jsonData = SerializeToJSON(data);

        if (jsonData == null)
        {
            Debug.LogError(String.Format("Failed to send game data on stream '{0}' due to a JSON serialization error.", streamID));
            return false;
        }

        return SubmitGameData(streamID, jsonData);
    }

    public bool SubmitAnnotationJSON(object streamID, object data)
    {
        if (data == null)
        {
            Debug.LogError("Unable to handle `null` data. Submitting annotation failed.");
            return false;
        }

        var jsonData = SerializeToJSON(data);

        if (jsonData == null)
        {
            Debug.LogError(String.Format("Failed to send annotation on stream '{0}' due to a JSON serialization error.", streamID));
            return false;
        }

        return SubmitAnnotation(streamID, jsonData);
    }

    [System.Obsolete("This is an obsolete overload, please consider using `SubmitAnnotationJSON` if you're submitting JSON serializable data.")]
    public bool SubmitAnnotation(object streamID, object data)
    {
        return SubmitAnnotationJSON(streamID, data);
    }

    [System.Obsolete("This is an obsolete overload, please consider using `SubmitGameDataJSON` if you're submitting JSON serializable data.")]
    public bool SubmitGameData(object streamID, object data)
    {
        return SubmitGameDataJSON(streamID, data);
    }

    [System.Obsolete("This method has been moved to the `GenvidSession` object as notifications are not streams.")]
    public bool SubmitNotification(object notificationID, object data)
    {
        return GenvidSessionManager.Instance.Session.SubmitNotification(notificationID, data);
    }

    [System.Obsolete("This method has been moved to the `GenvidSession` object as notifications are not streams.")]
    public bool SubmitNotification(object notificationID, string data)
    {
        return GenvidSessionManager.Instance.Session.SubmitNotification(notificationID, data);
    }

    internal static String SerializeToJSON(object data)
    {
        var jsonData = JsonUtility.ToJson(data);

        if (jsonData.Equals("{}") && !data.ToString().Equals(""))
        {
            Debug.LogError(String.Format("JSON serialization failed to handle: {0}", data.ToString()));
            return null;
        }

        return jsonData;
    }
}
