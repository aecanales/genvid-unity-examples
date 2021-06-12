using UnityEngine;

/// <summary>
/// This simple script rotates the cube at a constant speed and alternates its between white and another color.
/// </summary>
public class Cube : MonoBehaviour
{
    [Tooltip("Rotation speed for the cube defined in euler degrees per second.")]
    public Vector3 RotationSpeed;
    [Tooltip("The cube will alternate between white and this color.")]
    public Color TargetColor;
    [Range(0, 1)]
    public float ColorSpeed;

    private new Renderer renderer;

    void Start()
    {
        renderer = GetComponent<Renderer>();
    }

    void Update()
    {
        transform.Rotate(RotationSpeed * Time.deltaTime);
        renderer.material.color =  Color.Lerp(Color.white, TargetColor, Mathf.PingPong(Time.time * ColorSpeed, 1));;
    }
}