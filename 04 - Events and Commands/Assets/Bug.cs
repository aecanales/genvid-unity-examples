using UnityEngine;

// Script that moves the bug randomly and returns its collision box.
public class Bug : MonoBehaviour 
{
    private Vector3 startingPosition;
    private Vector3 finalPosition;
    
    private float speedModifier;

    void Start()
    {
        // We use Random to calculate the final position and a speed modifier.
        startingPosition = transform.position;
        finalPosition = startingPosition + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 1.5f));
        
        speedModifier = Random.Range(0.5f, 1.5f);
    }

    void Update()
    {
        // We use ping-pong and lerp to move between the two positions.
        float pingPong = Mathf.PingPong(Time.time * speedModifier, 1);
        transform.position = Vector3.Lerp(startingPosition, finalPosition, pingPong);
    }
}