using UnityEngine;

public class RedJump : MonoBehaviour
{
    [Header("Center Reference")]
    public Transform center;

    [Header("Jump Levels")]
    public float smallJump = 50f;
    public float mediumJump = 110f;
    public float bigJump = 180f;

    [Header("Charge Settings")]
    public float chargeSpeed = 200f;
    public float maxCharge = 180f;

    [Header("Movement Settings")]
    public float returnSpeed = 300f;
    public float jumpSmooth = 8f;

    [Header("Squash & Stretch")]
    public Vector3 idleScale = new Vector3(1f, 1f, 1f);
    public Vector3 chargeScale = new Vector3(1.1f, 0.85f, 1f);
    public Vector3 jumpStretchScale = new Vector3(0.85f, 1.25f, 1f);
    public Vector3 peakSquashScale = new Vector3(1.15f, 0.9f, 1f);
    public Vector3 landingSquashScale = new Vector3(1.25f, 0.75f, 1f);
    public float scaleSpeed = 15f;

    [Header("Jump Buffering")]
    public float jumpBufferTime = 0.12f;
    float jumpBufferCounter = 0f;

    [Header("Control Lock")]
    public bool controlsLocked = false;

    // Estado radial
    public float angle;      // << nuevo: el ángulo lo controla RedHitReaction
    float baseRadius;
    public float currentRadius;
    float targetRadius;

    // Estado de salto
    float charge = 0f;
    bool isCharging = false;
    bool isJumping = false;
    bool isReturning = false;
    bool isGrounded = true;
    int lastJumpLevel = 0;

    void Start()
    {
        baseRadius = Vector3.Distance(transform.position, center.position);
        currentRadius = baseRadius;
        targetRadius = baseRadius;

        // Inicializa ángulo exacto sin depender de la posición bruta
        Vector3 dir = (transform.position - center.position).normalized;
        angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    void Update()
    {
        if (controlsLocked) return;

        UpdateGrounded();
        HandleInput();
        UpdateJump();
        UpdatePosition();   // << nuevo
        UpdateOrientation();
        UpdateScale();
    }

    // ----- Actualización suave de la posición -----
    void UpdatePosition()
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad),0f) * currentRadius;

        transform.position = center.position + offset;
    }

    void UpdateGrounded()
    {
        bool nearBase = Mathf.Abs(currentRadius - baseRadius) < 0.5f;
        isGrounded = nearBase && !isJumping && !isReturning;
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            jumpBufferCounter = jumpBufferTime;

        if (jumpBufferCounter > 0f && isGrounded && !isCharging)
        {
            charge = 0f;
            isCharging = true;
            jumpBufferCounter = 0f;
        }

        if (isCharging && Input.GetKey(KeyCode.Space))
        {
            charge += chargeSpeed * Time.deltaTime;
            charge = Mathf.Clamp(charge, 0f, maxCharge);
        }

        if (isCharging && Input.GetKeyUp(KeyCode.Space))
        {
            if (isGrounded)
            {
                isCharging = false;
                DecideJumpLevel();
            }
            else
            {
                isCharging = false;
                charge = 0f;
            }
        }
    }

    void DecideJumpLevel()
    {
        isJumping = true;
        isReturning = false;

        if (charge < maxCharge * 0.33f)
        {
            targetRadius = baseRadius + smallJump;
            lastJumpLevel = 1;
        }
        else if (charge < maxCharge * 0.66f)
        {
            targetRadius = baseRadius + mediumJump;
            lastJumpLevel = 2;
        }
        else
        {
            targetRadius = baseRadius + bigJump;
            lastJumpLevel = 3;
        }
    }

    void UpdateJump()
    {
        if (isJumping)
        {
            currentRadius = Mathf.Lerp(currentRadius, targetRadius, jumpSmooth * Time.deltaTime);

            if (Mathf.Abs(currentRadius - targetRadius) < 1f)
            {
                isJumping = false;
                isReturning = true;
            }
        }
        else if (isReturning)
        {
            currentRadius = Mathf.MoveTowards(currentRadius, baseRadius, returnSpeed * Time.deltaTime);

            if (Mathf.Abs(currentRadius - baseRadius) < 0.05f)
            {
                currentRadius = baseRadius;
                isReturning = false;
                TriggerCameraShake();
            }
        }
    }

    void UpdateOrientation()
    {
        float rad = angle * Mathf.Deg2Rad;
        Vector3 radialDir = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
        transform.up = radialDir;
    }

    void UpdateScale()
    {
        Vector3 targetScale = idleScale;

        if (isCharging && isGrounded)
            targetScale = chargeScale;
        else if (isJumping)
            targetScale = jumpStretchScale;
        else if (isReturning)
        {
            if (Mathf.Abs(currentRadius - targetRadius) < 3f)
                targetScale = peakSquashScale;
            else if (Mathf.Abs(currentRadius - baseRadius) < 0.3f)
                targetScale = landingSquashScale;
        }

        transform.localScale =
            Vector3.Lerp(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
    }

    public void InterruptJump()
    {
        if (!isJumping) return;

        isJumping = false;
        isReturning = true;
        targetRadius = baseRadius;
        isCharging = false;
        charge = 0f;
    }

    void TriggerCameraShake()
    {
        if (lastJumpLevel != 3) return;
        if (Camera.main == null) return;

        CameraShake cam = Camera.main.GetComponent<CameraShake>();
        if (cam != null)
            cam.Shake(0.10f, 2.5f);
    }

}
