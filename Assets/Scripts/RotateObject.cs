using UnityEngine;

public class RotateObject : MonoBehaviour
{
    [Tooltip("Grados por segundo")]
    public float rotationSpeed = 45f;

    void Update()
    {
        // Gira en el eje Z (2D)
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}
