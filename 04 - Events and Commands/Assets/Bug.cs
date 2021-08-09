using UnityEngine;

public class Bug : MonoBehaviour 
{
    public Vector2 MovementVector;
    
    private Vector3 startingPosition;
    private Vector3 finalPosition;

    void Start()
    {
        startingPosition = transform.position;
        finalPosition = startingPosition + (Vector3) MovementVector;
    }

    void Update()
    {
        float pingPong = Mathf.PingPong(Time.time, 1);
        transform.position = Vector3.Lerp(startingPosition, finalPosition, pingPong);
    }
}