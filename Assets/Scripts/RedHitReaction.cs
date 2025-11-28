using UnityEngine;

public class RedHitReaction : MonoBehaviour
{
    [Header("References")]
    public Transform center;             
    public RedJump redJump;

    [Header("Angular Knockback Settings")]
    public float knockbackAngleAmount = 40f;  // grados hacia atrás
    public float knockbackAngularSpeed = 160f; // grados por segundo
    public float angleFriction = 10f;        // suavizado al final

    [Header("Protection")]
    public float hitCooldown = 0.25f;
    float lastHitTime = -99f;

    [Header("Optional Effects")]
    public bool interruptJumpOnHit = true;
    public bool useCameraShake = true;
    public float shakeDuration = 0.08f;
    public float shakeMagnitude = 0.07f;

    float targetAngle;               // hacia dónde debe llegar
    bool isKnockbackActive = false;

    // --- Recovery System ---
    [Header("Recovery Settings")]
    public float recoverySpeed = 25f;   // velocidad para volver al ángulo inicial

    float originalAngle;                // donde empezó el jugador
    bool isRecovering = false;          // está regresando?

    // fuerza del último golpe (normalizada)
    float lastHitStrength = 1f;



    void Start()
    {
        // Inicializamos targetAngle con el ángulo real actual
        targetAngle = GetCurrentAngle();
        // guardar posición angular inicial
        originalAngle = targetAngle;
    }

    void Update()
    {
        if (isKnockbackActive)
            ProcessAngularKnockback();

        if (!isKnockbackActive && isRecovering)
        {
            float currentAngle = NormalizeAngle(GetCurrentAngle());

            float scaledRecovery = recoverySpeed / Mathf.Pow(lastHitStrength, 1.4f);

            float newAngle = Mathf.MoveTowardsAngle(
                currentAngle,
                originalAngle,
                scaledRecovery * Time.deltaTime
            );

            UpdatePositionFromAngle(newAngle);

            if (Mathf.Abs(Mathf.DeltaAngle(newAngle, originalAngle)) < 0.5f)
                isRecovering = false;
        }

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Wave")) return;

        if (Time.time - lastHitTime < hitCooldown)
            return;
            
        var ctrl = other.GetComponentInParent<WaveController>();
        if (ctrl != null)
            ctrl.hitPlayer = true;

        lastHitTime = Time.time;

        // fuerza por defecto
        float strength = 1f;

        // obtener fuerza real desde la onda
        var wave = other.GetComponent<CircularSineWave>();
        if (wave != null)
        {
            // normalizar amplitud → ajusta 80f según tus valores
            strength = Mathf.Clamp(wave.amplitude / 80f, 0.3f, 3f);
        }

        ApplyAngularKnockback(strength);
    }

    void ApplyAngularKnockback(float hitStrength)
    {
        if (interruptJumpOnHit)
            redJump.InterruptJump();

        float currentAngle = NormalizeAngle(GetCurrentAngle());
        targetAngle = NormalizeAngle(currentAngle + knockbackAngleAmount);

        isKnockbackActive = true;

        // cancelar recuperación si estaba volviendo
        isRecovering = false;
        lastHitStrength = hitStrength;

        // cámara shake opcional
        if (useCameraShake && Camera.main != null)
        {
            CameraShake cam = Camera.main.GetComponent<CameraShake>();
            if (cam != null)
                cam.Shake(shakeDuration, shakeMagnitude);
        }
    }

    void ProcessAngularKnockback()
    {
        float currentAngle = NormalizeAngle(GetCurrentAngle());
        float tAngle = NormalizeAngle(targetAngle);

        // mover hacia targetAngle
        float step = knockbackAngularSpeed * Time.deltaTime;
        float newAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, step);

        UpdatePositionFromAngle(newAngle);

        if (Mathf.Abs(Mathf.DeltaAngle(newAngle, targetAngle)) < 0.5f)
        {
            isKnockbackActive = false;
            isRecovering = true; // empezar retorno
        }
    }

    float GetCurrentAngle()
    {
        Vector3 dir = (transform.position - center.position).normalized;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    void UpdatePositionFromAngle(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        float r = (transform.position - center.position).magnitude;

        Vector3 newPos = center.position + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * r;
        transform.position = newPos;
    }

    float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0) angle += 360f;
        return angle;
    }

}
