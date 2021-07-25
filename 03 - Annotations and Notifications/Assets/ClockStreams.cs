using UnityEngine;

// This class sends the corresponding notifications and annotations.
// This class is added to the GenvidStreams object and two streams are created.
public class ClockStreams : MonoBehaviour
{
    // For this example, we'll just be sending a very simple object that just contains a string with current time.
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
        // We send the notification/annotation every 15 seconds.
        int second = System.DateTime.Now.Second;
        if (second % 15 == 0 && second != previousSecond)
        {
            // Note here how we are sending the notification and the annotation at the same time.
            canSendAnnotation = true;
            canSendNotification = true;
            previousSecond = second;
        }
    }

    public void SubmitTimeChangeAnnotation(string streamId)
    {
        // Even if we want to send an annotation/notification at a specific moment of gameplay, the GenvidStreams object
        // will always call this method according to the frequency set in the  inspector. Because of this, 
        // we must  use something like a boolean to make sure it's only sent at the correct time.
        if (!canSendAnnotation)
            return;
        
        if (GenvidSessionManager.IsInitialized && GenvidSessionManager.Instance.enabled)
        {
            Message message = new Message { Content = System.DateTime.Now.ToString("hh:mm:ss tt") };

            // We use SubmitAnnotationJSON to send an annotation. 
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

            // We use SubmitNotificationJSON to send a notification. Note how GenvidStreams object does not
            // differentiate between data streams, annotations or notifications in the inspector, 
            // and the difference only depends on the method we call in our code.
            GenvidSessionManager.Instance.Session.SubmitNotificationJSON(streamId, message);

            canSendNotification = false;
        }
    }
}