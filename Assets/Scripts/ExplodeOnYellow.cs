using System.Collections;
using UnityEngine;

public class ExplodeOnYellow : MonoBehaviour
{
    [Header("Prefab del sistema de partículas")]
    public ParticleSystem explosionPrefab;

    [Header("References")]
    public RedJump redJump;              // referencia al script de salto
    public RedHitReaction redHit;        // referencia al knockback angular (opcional)

    [Header("Saw Kill Settings")]
    public float preBreakDuration = 0.6f;    // cuánto tiempo vibra antes de romperse
    public bool useCameraShake = true;
    public float cameraShakeStepDuration = 0.08f;
    public float cameraShakeBaseMagnitude = 0.15f;
    public float cameraShakeMaxMagnitude = 0.45f;
    public float bodyShakeAmount = 0.12f;

    bool isDying = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // que solo se ejecute una vez
        if (isDying) return;

        // el círculo amarillo/sierra debe tener tag "Enemy"
        if (other.CompareTag("Enemy"))
        {
            StartCoroutine(SawKillSequence());
        }
    }

    IEnumerator SawKillSequence()
    {
        isDying = true;

        // 1) bloquear control y salto
        if (redJump != null)
        {
            redJump.InterruptJump();      // corta un salto en curso
            redJump.controlsLocked = true;
        }

        // 2) desactivar reacciones de golpes para que no mueva la posición
        if (redHit != null)
        {
            redHit.enabled = false;
        }

        // 3) vibración del cuerpo y cámara
        Vector3 basePos = transform.position;
        float t = 0f;

        while (t < preBreakDuration)
        {
            t += Time.deltaTime;
            float normalized = Mathf.Clamp01(t / preBreakDuration);

            // vibración del cuerpo alrededor de su punto
            Vector2 offset = Random.insideUnitCircle * bodyShakeAmount * (1f - normalized);
            transform.position = basePos + (Vector3)offset;

            // vibración de la cámara, intensidad creciente
            if (useCameraShake && Camera.main != null)
            {
                CameraShake cam = Camera.main.GetComponent<CameraShake>();
                if (cam != null)
                {
                    float mag = Mathf.Lerp(cameraShakeBaseMagnitude, cameraShakeMaxMagnitude, normalized);
                    cam.Shake(cameraShakeStepDuration, mag);
                }
            }

            yield return null;
        }

        // aseguramos que vuelva al punto base justo antes de explotar
        transform.position = basePos;

        // 4) explosión de partículas
        if (explosionPrefab != null)
        {
            ParticleSystem explosion = Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );
            explosion.Play();
        }

        // 5) destruir el jugador
        Destroy(gameObject);

    }
}
