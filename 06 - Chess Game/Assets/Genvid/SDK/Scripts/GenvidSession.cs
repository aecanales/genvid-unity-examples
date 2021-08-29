using GenvidSDKCSharp;
using UnityEngine;
using System;
using System.Collections;
using System.Text;

public class GenvidSession : MonoBehaviour
{
    public GenvidVideo VideoStream;
    public GenvidAudio AudioStream;
    public GenvidStreams Streams;
    public GenvidEvents Events;
    public GenvidCommands Commands;
    private bool m_IsCreated = false;

    public void Create()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (GenvidSessionManager.Instance.ActivateSDK && !m_IsCreated && GenvidSessionManager.IsInitialized)
        {
            if (VideoStream != null)
            {
                if(VideoStream.Create() == false)
                {
                    Debug.LogError("GenvidSession failed to create a video stream!");
                }
            }
            else
            {
                Debug.LogWarning("GenvidSession does not have a GenvidVideo GameObject linked to it!");
            }

            if (AudioStream != null)
            {
                if(AudioStream.Create() == false)
                {
                    Debug.LogError("GenvidSession failed to create an audio stream!");
                }
            }
            else
            {
                Debug.LogWarning("GenvidSession does not have a GenvidVideo GameObject linked to it!");
            }

            if (Streams != null)
            {
                if(Streams.Create() == false)
                {
                    Debug.LogError("GenvidSession failed to create gamedata streams!");
                }
            }
            else
            {
                Debug.LogWarning("GenvidSession does not have a GenvidStreams GameObject linked to it!");
            }

            if (Events != null)
            {
                Events.Create();
            }
            
            if (Commands != null)
            {
                Commands.Create();
            }
            
            m_IsCreated = true;
        }
    #endif
    }

    public bool SubmitNotification(object notificationID, ref byte[] data, int size)
    {
        if (!m_IsCreated)
        {
            if (GenvidSessionManager.Instance.ActivateSDK && !GenvidSessionManager.IsInitialized)
            {
                Debug.LogError("Genvid SDK is not initialized: unable to submit notification.");
            }
            else
            {
                Debug.LogError(String.Format("Unable to submit notification with ID '{0}'.", notificationID));
            }

            return false;
        }

        var status = GenvidSDK.SubmitNotification(notificationID.ToString(), data, size);

        if (GenvidSDK.StatusFailed(status))
        {
            Debug.LogError(String.Format("`SubmitNotitication` failed with error: {0}", GenvidSDK.StatusToString(status)));
            return false;
        }

        if (GenvidSessionManager.Instance.ActivateDebugLog)
        {
            Debug.Log(String.Format("Genvid correctly submitted notification: {0}", data));
        }

        return true;
    }

    public bool SubmitNotification(object notificationID, ref byte[] data)
    {
        return SubmitNotification(notificationID, ref data, data.Length);
    }

    public bool SubmitNotification(object notificationID, string data)
    {
        if (data == null)
        {
            Debug.LogError("Unable to handle `null` data. Submitting notification failed.");
            return false;
        }

        var dataAsBytes = Encoding.Default.GetBytes(data);
        return SubmitNotification(notificationID, ref dataAsBytes);
    }

    public bool SubmitNotificationJSON(object notificationID, object data)
    {
        if (data == null)
        {
            Debug.LogError("Unable to handle `null` data. Submitting notification failed.");
            return false;
        }

        var jsonData = GenvidStreams.SerializeToJSON(data);

        if (jsonData == null)
        {
            Debug.LogError(String.Format("Failed to send notification with ID '{0}' due to a JSON serialization error.", notificationID));
            return false;
        }

        return SubmitNotification(notificationID, jsonData);
    }

    [System.Obsolete("This is an obsolete overload, please consider using `SubmitNotificationJSON` if you're submitting JSON serializable data.")]
    public bool SubmitNotification(object notificationID, object data)
    {
        return SubmitNotificationJSON(notificationID, data);
    }

    public void Destroy()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (GenvidSessionManager.Instance.ActivateSDK && m_IsCreated)
        {
            Commands.Destroy();
            Events.Destroy();
            Streams.Destroy();            
            AudioStream.Destroy();
            VideoStream.Destroy();
            m_IsCreated = false;
        }
    #endif
    }
}
