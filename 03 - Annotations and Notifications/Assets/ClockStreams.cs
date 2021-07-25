using UnityEngine;

public class ClockStreams : MonoBehaviour
{
    [System.Serializable]
    public struct Message
    {
        public string Content;
    }

    int previousSecond;
    bool canSendAnnotation;
    bool canSendNotification;
    
    void Start()
    {
        previousSecond = System.DateTime.Now.Second;
    }

    void Update()
    {
        int second = System.DateTime.Now.Second;
        if (second % 15 == 0 && second != previousSecond)
        {
            canSendAnnotation = true;
            canSendNotification = true;
            previousSecond = second;
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