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
    public struct GameData
    {
        [SerializeField] public float CubeRotationX;
        [SerializeField] public float CubeRotationY;
        [SerializeField] public float CubeRotationZ;
        [SerializeField] public float CubeColorR;
        [SerializeField] public float CubeColorG;
        [SerializeField] public float CubeColorB;
    }

    // This method must be called by the GenvidStreams object to make sure that data is submitted.
    public void SubmitGameData(string streamId)
    {
        // Before submitting any data, we must always check whether the session is running.
        if (GenvidSessionManager.IsInitialized && GenvidSessionManager.Instance.enabled)
        {
            // We create a GameData object with the information we want to submit.
            GameData gameData = new GameData() {
                CubeRotationX = Cube.transform.localEulerAngles.x,
                CubeRotationY = Cube.transform.localEulerAngles.y,
                CubeRotationZ = Cube.transform.localEulerAngles.z,
                CubeColorR = cubeRenderer.material.color.r,
                CubeColorG = cubeRenderer.material.color.g,
                CubeColorB = cubeRenderer.material.color.b
            };

            // We submit the data as a data stream.
            GenvidSessionManager.Instance.Session.Streams.SubmitGameDataJSON(streamId, gameData);
        }
    }
}