using UnityEngine;

// Sends the bounding boxes of each bug so that the web view player can click on them.
public class BugStream : MonoBehaviour
{
    public GameObject BugContainer;

    private Bug[] bugs;

    void Start()
    {
        bugs = BugContainer.GetComponentsInChildren<Bug>();
    }

    // This struct will contain a list of bounding boxes to send via a JSON data stream.
    [System.Serializable]
    public struct BugData
    {
        [SerializeField] public Bug.BoundingBox[] BoundingBoxes;
    }

    // This method must be called by the GenvidStreams object to make sure that data is submitted.
    public void SendBugInformationStream(string streamId)
    {
        if (GenvidSessionManager.IsInitialized && GenvidSessionManager.Instance.enabled)
        {
            // We create the list of bounding boxes we want to send and create a InteractionSpotData struct.
            Bug.BoundingBox[] list = new Bug.BoundingBox[bugs.Length];

            for (int i = 0; i < bugs.Length; i++)
            {
                list[i] = bugs[i].GetBoundingBox();
            }

            BugData spotData = new BugData() {
                BoundingBoxes = list
            };

            GenvidSessionManager.Instance.Session.Streams.SubmitGameDataJSON(streamId, spotData);
        }
    }
}