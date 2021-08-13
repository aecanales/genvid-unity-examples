using UnityEngine;

// Script that moves the bug randomly and returns its collision box.
public class Bug : MonoBehaviour 
{
    // To be able to identify which bug the viewers have clicked, we assign each bug a unique id.
    // To do that, we use a static variable which is shared between all instances of Bug.
    private static int ID_COUNTER = 0;
    public int Id;
    
    private Vector3 startingPosition;
    private Vector3 finalPosition;
    
    private float speedModifier;
    private BoxCollider2D boxCollider2D;

    void Start()
    {
        Id = ID_COUNTER;
        ID_COUNTER++;
        
        // We use Random to calculate the final position and a speed modifier.
        startingPosition = transform.position;
        finalPosition = startingPosition + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 1.5f));
        
        speedModifier = Random.Range(0.5f, 1.5f);

        boxCollider2D = GetComponent<BoxCollider2D>();
    }

    void Update()
    {
        // We use ping-pong and lerp to move between the two positions.
        float pingPong = Mathf.PingPong(Time.time * speedModifier, 1);
        transform.position = Vector3.Lerp(startingPosition, finalPosition, pingPong);
    }

    // The bounding box gives us information on the clickable region of this interaction spot in 
    // *screen coordinates*.
    [System.Serializable]
    public struct BoundingBox
    {
        [SerializeField] public int ID;
        // The X and Y correspond to the top left corner of the box.
        [SerializeField] public float X;
        [SerializeField] public float Y;
        [SerializeField] public float Width;
        [SerializeField] public float Height;
    }      
    
    public BoundingBox GetBoundingBox()
    {
        Bounds bounds = boxCollider2D.bounds;
        Camera camera = Camera.main;

        Vector3 topRight = camera.WorldToScreenPoint(transform.position + bounds.extents);
        Vector3 bottomLeft = camera.WorldToScreenPoint(transform.position - bounds.extents);
        
        // WorldToScreenPoint places (0,0) in the bottom left of the screen, whereas the web view
        // places (0,0) in the *top* left, so we must transform the Y.
        return new BoundingBox() {
            ID = Id,
            X = bottomLeft.x,
            Y = Screen.height - topRight.y,
            Width = topRight.x - bottomLeft.x,
            Height = topRight.y - bottomLeft.y,
        };
    }   
}