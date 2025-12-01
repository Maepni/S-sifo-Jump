using UnityEngine;

public class WaveController : MonoBehaviour
{
    public CircularSineWave sine;
    public float speed = 60f;      // grados por segundo
    public float lifetime = 0f;    // si es 0, se calcula automático
    public bool hitPlayer = false;

    void Start()
    {
        if (sine != null)
        {
            // nos aseguramos de que la dirección sea antihoraria
            sine.speed = Mathf.Abs(speed);

            // calcular lifetime si no se ha fijado en el inspector
            if (lifetime <= 0f)
            {
                float span = Mathf.Abs(sine.angleSpan); // normalmente 360
                lifetime = span / sine.speed + 0.5f;    // + margen pequeño
            }
        }

        Destroy(gameObject, lifetime);
    }
}
