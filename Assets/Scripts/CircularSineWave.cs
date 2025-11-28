using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class CircularSineWave : MonoBehaviour
{
    public Transform center;
    public float radius = 3f;
    public int points = 256;

    public float amplitude = 15f;
    public float waveWidth = 6;     // ancho de la ola
    public float angleSpan = 360f;      // círculo completo
    [HideInInspector]
    public float speed;
    public bool passedPlayer = false;
    public float passThreshold = 8f; // en grados


    private float currentAngle = 0f;

    private LineRenderer lr;
    private EdgeCollider2D edge;
    private Vector3[] positions;
    public float minAngleBeforeScoring = 20f;
    private float initialAngle;


    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        edge = GetComponent<EdgeCollider2D>();

        lr.useWorldSpace = false;
        positions = new Vector3[points];
        lr.positionCount = points;
    }
    void Update()
    {
        if (!center) return;

        transform.position = center.position;

        // mover la ola angularmente
        currentAngle += speed * Time.deltaTime;

        for (int i = 0; i < points; i++)
        {
            float t = (float)i / (points - 1);

            float angleDeg = t * angleSpan;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            Vector3 basePos = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad), 0f) * radius;

            float diff = Mathf.DeltaAngle(angleDeg, currentAngle);

            // pulso suave, UNA sola onda
            float wave = Mathf.Exp(-(diff * diff) / (2f * waveWidth * waveWidth)) * amplitude;

            // mover radialmente solo donde el pulso existe
            Vector3 finalPos = basePos + basePos.normalized * wave;

            positions[i] = finalPos;
        }

        lr.SetPositions(positions);

        // actualizar colisión
        Vector2[] pts2D = new Vector2[points];
        for (int i = 0; i < points; i++)
            pts2D[i] = positions[i];

        edge.points = pts2D;

        CheckPlayerPass();
    }

    void CheckPlayerPass()
    {
        if (passedPlayer) return;

        // Impedir que detecte inmediatamente al aparecer
        float traveled = Mathf.Abs(Mathf.DeltaAngle(initialAngle, currentAngle));
        if (traveled < minAngleBeforeScoring)
            return;

        passedPlayer = true;
    }

}
