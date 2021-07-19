using UnityEngine;

public class ClockStreams : MonoBehaviour
{
    [System.Serializable]
    public struct Message
    {
        public string Content;
    }

    int previousMinute;
    bool canSendAnnotation;
    bool canSendNotification;
    
    void Start()
    {
        previousMinute = System.DateTime.Now.Minute;
    }

    void Update()
    {
        int minute = System.DateTime.Now.Minute;
        if (minute != previousMinute)
        {
            canSendAnnotation = true;
            canSendNotification = true;
            previousMinute = minute;
        }
    }

    public void SubmitTimeChangeAnnotation(string streamId)
    {
        if (!canSendAnnotation)
            return;
        
        if (GenvidSessionManager.IsInitialized && GenvidSessionManager.Instance.enabled)
        {
            Message message = new Message { Content = System.DateTime.Now.ToString("hh:mm:ss tt") };

            GenvidSessionManager.Instance.Session.Streams.SubmitAnnotationJSON(streamId, message);

            canSendAnnotation = false;
        }
    }

    public void SubmitTimeChangeNotification(string streamId)
    {
        if (!canSendNotification)
            return;

        if (GenvidSessionManager.IsInitialized && GenvidSessionManager.Instance.enabled)
        {
            Message message = new Message { Content = System.DateTime.Now.ToString("hh:mm:ss tt") };

            GenvidSessionManager.Instance.Session.SubmitNotificationJSON(streamId, message);

            canSendNotification = false;
        }
    }
}