using UnityEngine;
using System.Collections;
using System;
using GenvidSDKCSharp;
using System.Collections.Generic;
using UnityEngine.Events;

public class GenvidEvents : MonoBehaviour
{
    [Serializable]
    public class GenvidEventType : UnityEvent<string, GenvidSDK.EventResult[], int, IntPtr>
    {
    }

    [Serializable]
    public class GenvidEventElement
    {
        public string Id;
        public GenvidEventType OnEventTriggered;
    }

    private class EventDataFunction
    {
        public GenvidEventType Callback;
        public GenvidSDK.EventSummary Summary;
        public IntPtr UserData;
    }

    public GenvidEventElement[] Ids;

    // Variables excluded to prevent warnings
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    private GenvidSDK.EventSummaryCallback m_EventCallback = null;
    private Dictionary<string, IntPtr> m_EventData = null;
    private Stack<EventDataFunction> m_EventPool = null;
    private bool m_IsCreated = false;
#endif

    public void Create()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (GenvidSessionManager.Instance.ActivateSDK && !m_IsCreated && GenvidSessionManager.IsInitialized)
        {
            m_EventData = new Dictionary<string, IntPtr>();
            m_EventPool = new Stack<EventDataFunction>();
            m_EventCallback = new GenvidSDK.EventSummaryCallback(EventCallbackFunction);

            int index = 0;
            foreach (var ev in Ids)
            {
                var userData = new IntPtr(index);
                var status = GenvidSDK.Subscribe(ev.Id, m_EventCallback, userData);
                if (GenvidSDK.StatusFailed(status))
                {
                    Debug.LogError("Error while Subscribing the " + ev.Id + " event: " + GenvidSDK.StatusToString(status));
                }
                else
                {
                    if (GenvidSessionManager.Instance.ActivateDebugLog)
                    {
                        Debug.Log("Genvid Subscribing event named " + ev.Id + " performed correctly.");
                    }
                    m_EventData.Add(ev.Id, userData);
                    ++index;
                }
            }
            m_IsCreated = true;
        }
    #endif
    }

	public void Destroy()
    {
    #if UNITY_EDITOR || UNITY_STANDALONE_WIN
        if (GenvidSessionManager.Instance.ActivateSDK && m_IsCreated)
        {
            foreach (var eventName in m_EventData)
            {
                var status = GenvidSDK.Unsubscribe(eventName.Key, m_EventCallback, eventName.Value);
                if (GenvidSDK.StatusFailed(status))
                {
                    Debug.LogError("Error while unsubscribing the " + eventName.Key + " event: " + GenvidSDK.StatusToString(status));
                }
                else if (GenvidSessionManager.Instance.ActivateDebugLog)
                {
                    Debug.Log("Genvid Unsubscribing event named " + eventName.Key + " performed correctly.");
                }
            }

            m_EventData.Clear();
            m_EventPool.Clear();
            m_EventPool = null;
            m_EventData = null;
            m_EventCallback = null;
            m_IsCreated = false;
        }
    #endif
    }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    void EventCallbackFunction(IntPtr summaryData, IntPtr userData)
    {
        var summary = GenvidSDK.GetSummary(summaryData);

        if (m_EventPool == null)
        {
            Debug.Log("Event '" + summary.id + "' has been ignored because the Event Pool is null.");
        }
        
        foreach(var ev in Ids)
        {
            if (ev.Id == summary.id)
            {
                if (ev.OnEventTriggered != null)
                {
                    try
                    {
                        EventDataFunction dataEvent = new EventDataFunction();
                        dataEvent.Callback = ev.OnEventTriggered;
                        dataEvent.Summary = summary;
                        dataEvent.UserData = userData;
                        m_EventPool.Push(dataEvent);
                    }
                    catch(OutOfMemoryException ex)
                    {
                        Debug.LogError(ex.Message);
                    }
                }
                break;
            }
        }
    }

    private void FixedUpdate()
    {
        if(m_EventPool != null && m_IsCreated)
        {
            while (m_EventPool.Count > 0)
            {
                var dataEvent = m_EventPool.Pop();
                dataEvent.Callback.Invoke(dataEvent.Summary.id, dataEvent.Summary.results, dataEvent.Summary.numResults, dataEvent.UserData);
            }
        }
    }
#endif
}
