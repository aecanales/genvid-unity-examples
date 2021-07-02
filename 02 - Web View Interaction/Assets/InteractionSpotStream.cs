using UnityEngine;

public class InteractionSpotStream : MonoBehaviour
{
    [Tooltip("Game object that has the text with interaction spots as their children.")]
    public GameObject LabelContainer;

    private InteractionSpot[] spots;
    private bool drawDebug;

    void Start()
    {
        spots = LabelContainer.GetComponentsInChildren<InteractionSpot>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            drawDebug = !drawDebug;
        }   
    }

    // To test whether the bounding boxes are being correctly calculated, we have this simple
    // debug tool that draws the bounding boxes via Unity GUI.
    // Note that the default Genvid camera does *not* capture GUI and thus this will not appear on the stream.
    void OnGUI()
    {
        if (drawDebug)
        {
            foreach(InteractionSpot spot in spots)
            {
                InteractionSpot.BoundingBox bbox = spot.GetBoundingBox();
                
                GUI.Box(new Rect(bbox.X, bbox.Y, bbox.Width, bbox.Height), "");
            }
        }
    }

    // This struct will contain a list of bounding boxes to send via a JSON data stream.
    [System.Serializable]
    public struct InteractionSpotData
    {
        [SerializeField] public InteractionSpot.BoundingBox[] BoundingBoxes;
    }

    // This method must be called by the GenvidStreams object to make sure that data is submitted.
    public void SendInteractionSpotStream(string streamId)
    {
        if (GenvidSessionManager.IsInitialized && GenvidSessionManager.Instance.enabled)
        {
            // We create the list of bounding boxes we want to send and create a InteractionSpotData struct.
            InteractionSpot.BoundingBox[] list = new InteractionSpot.BoundingBox[spots.Length];

            for (int i = 0; i < spots.Length; i++)
            {
                list[i] = spots[i].GetBoundingBox();
            }

            InteractionSpotData spotData = new InteractionSpotData() {
                BoundingBoxes = list
            };

            GenvidSessionManager.Instance.Session.Streams.SubmitGameDataJSON(streamId, spotData);
        }
    }
}