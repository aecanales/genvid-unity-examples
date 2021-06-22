using UnityEngine;

// This class defines the bounding box the web view user has to click inside to interact with this spot.
// It also contains the description the user will be able to read after interacting.
public class InteractionSpot : MonoBehaviour 
{
    [TextArea]
    public string Description;

    public BoundingBox GetBoundingBox()
    {
        Bounds bounds = GetComponent<BoxCollider2D>().bounds;
        Camera camera = Camera.main;

        Vector3 topRight = camera.WorldToScreenPoint(transform.position + bounds.extents);
        Vector3 bottomLeft = camera.WorldToScreenPoint(transform.position - bounds.extents);
        
        // WorldToScreenPoint places (0,0) in the bottom left of the screen, whereas the web view
        // and the Unity GUI system places (0,0) in the *top* left, so we must transform the Y.
        return new BoundingBox() {
            X = bottomLeft.x,
            Y = Screen.height - topRight.y,
            Width = topRight.x - bottomLeft.x,
            Height = topRight.y - bottomLeft.y,
            Text = Description
        };
    } 

    // The bounding box gives us information on the clickable region of this interaction spot in 
    // *screen coordinates*.
    [System.Serializable]
    public struct BoundingBox
    {
        // The X and Y correspond to the top left corner of the box.
        [SerializeField] public float X;
        [SerializeField] public float Y;
        [SerializeField] public float Width;
        [SerializeField] public float Height;
        [SerializeField] public string Text;
    }
}
