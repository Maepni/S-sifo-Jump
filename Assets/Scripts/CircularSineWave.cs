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
        if (ScoreManager.Instance == null) return;
        if (ScoreManager.Instance.player == null) return;
        // Impedir score si la onda recién apareció (protege contra spawns alineados)
        float traveled = Mathf.Abs(Mathf.DeltaAngle(initialAngle, currentAngle));
        if (traveled < minAngleBeforeScoring)
            return;

        // --- ÁNGULO DEL JUGADOR (0..360) ---
        Vector3 dir = (ScoreManager.Instance.player.position - center.position).normalized;
        float playerAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // ============================================================
        // 1) BUSCAR EL PUNTO DE LA ONDA MÁS CERCANO A ESE ÁNGULO
        //    USANDO positions[] QUE ES LA FORMA REAL DEL COLLIDER
        // ============================================================
        int bestIndex = -1;
        float bestDiff = float.MaxValue;

        for (int i = 0; i < points; i++)
        {
            float t = (float)i / (points - 1);
            float angleDeg = t * angleSpan;

            float d = Mathf.Abs(Mathf.DeltaAngle(angleDeg, playerAngle));
            if (d < bestDiff)
            {
                bestDiff = d;
                bestIndex = i;
            }
        }

        // si todavía está lejos angularmente, no hacemos nada
        if (bestIndex < 0 || bestDiff > passThreshold)
            return;

        // ============================================================
        // 2) RADIO REAL DE LA ONDA EN LA DIRECCIÓN DEL JUGADOR
        //    (positions está en coordenadas LOCALES respecto al centro)
        // ============================================================
        float waveR = positions[bestIndex].magnitude;

        // ============================================================
        // 3) RADIO REAL DEL JUGADOR
        // ============================================================
        float playerR = (ScoreManager.Instance.player.position - center.position).magnitude;

        bool alreadyOverlappingRadially = playerR <= waveR;

        // Nos aseguramos de que solo se procese UNA vez esta onda
        passedPlayer = true;

        var ctrl = GetComponent<WaveController>();

        // Si radialmente ya está dentro del pulso, consideramos que GOLPEA,
        // así que NO damos score.
        if (alreadyOverlappingRadially)
        {
            if (ctrl != null)
                ctrl.hitPlayer = true;

            return;
        }

        // Si NO golpeó (ni radialmente ni por hit previo), entonces damos score
        if (ctrl != null && !ctrl.hitPlayer)
        {
            ScoreManager.Instance.RegisterWaveEvaded();
        }
    }


}
