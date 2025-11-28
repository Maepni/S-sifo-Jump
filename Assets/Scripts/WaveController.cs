using UnityEngine;

public class WaveController : MonoBehaviour
{
    public CircularSineWave sine;
    public float speed = 60f;
    public float lifetime = 3f;
    public bool hitPlayer = false;

    void Start()
    {
        if (sine != null)
            sine.speed = speed;

        Destroy(gameObject, lifetime);
    }
}
