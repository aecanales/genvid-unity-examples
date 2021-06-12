using UnityEngine;

/// <summary>
/// The following script submits the information we want to be show on the web overlay 
/// via data streams to the Genvid Session Manager.
/// </summary>
public class DataStream : MonoBehaviour
{
    public GameObject Cube;

    private Renderer cubeRenderer;

    void Start()
    {
        cubeRenderer = Cube.GetComponent<Renderer>();
    }
    
    // We create a struct with the data we want to send so we can later serialize and send it as a JSON to our web overlay.
    [System.Serializable]
    private struct GameData
    {
        [SerializeField] public Vector3 CubeRotation;
        [SerializeField] public Color CubeColor;
    }

    // This method must be called by the GenvidStreams object to make sure that data is submitted.
    public void SubmitGameData(string streamId)
    {
        // Before submitting any data, we must always check whether the session is running.
        if (GenvidSessionManager.IsInitialized && GenvidSessionManager.Instance.enabled)
        {
            // We create a GameData object with the information we want to submit.
            GameData gameData = new GameData() {
                CubeRotation = Cube.transform.localEulerAngles,
                CubeColor = cubeRenderer.material.color
            };

            // We submit the data as a data stream.
            GenvidSessionManager.Instance.Session.Streams.SubmitGameDataJSON(streamId, gameData);
        }
    }
}