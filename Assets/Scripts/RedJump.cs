using UnityEngine;

public class RedJump : MonoBehaviour
{
    [Header("Center Reference")]
    public Transform center;      // círculo azul

    [Header("Jump Levels (distancia radial extra)")]
    public float smallJump = 50f;
    public float mediumJump = 110f;
    public float bigJump = 180f;

    [Header("Charge Settings")]
    public float chargeSpeed = 200f;   // qué tan rápido se carga
    public float maxCharge = 180f;     // límite de carga

    [Header("Movement Settings")]
    public float returnSpeed = 300f;   // qué tan rápido cae
    public float jumpSmooth = 8f;      // suavidad de subida

    [Header("Squash & Stretch")]
    public Vector3 idleScale = new Vector3(1f, 1f, 1f);
    public Vector3 chargeScale = new Vector3(1.1f, 0.85f, 1f);
    public Vector3 jumpStretchScale = new Vector3(0.85f, 1.25f, 1f);
    public Vector3 peakSquashScale = new Vector3(1.15f, 0.9f, 1f);
    public Vector3 landingSquashScale = new Vector3(1.25f, 0.75f, 1f);
    public float scaleSpeed = 15f;

    [Header("Jump Buffering")]
    public float jumpBufferTime = 0.12f;  // 120 ms recomendado
    float jumpBufferCounter = 0f;

    [Header("Control Lock")]
    public bool controlsLocked = false;



    // estado radial
    float baseRadius;
    float currentRadius;
    float targetRadius;

    // estado de salto / suelo
    float charge = 0f;
    bool isCharging = false;
    bool isJumping = false;
    bool isReturning = false;
    bool isGrounded = true;
    int lastJumpLevel = 0; 

    void Start()
    {
        if (center == null)
        {
            Debug.LogWarning("RedJump: asigna el Transform del círculo azul en 'center'.");
            enabled = false;
            return;
        }

        baseRadius = Vector3.Distance(transform.position, center.position);
        currentRadius = baseRadius;
        targetRadius = baseRadius;

        // orientación inicial correcta
        UpdateOrientation();
    }
    
    void Update()
    {
        if (controlsLocked)
            return;

        UpdateGrounded();
        HandleInput();
        UpdateJump();
        UpdateOrientation();
        UpdateScale();
    }


    // ---------- ESTADO DE SUELO ----------
    void UpdateGrounded()
    {
        // está en el suelo si está en el radio base y no está saltando ni volviendo
        bool nearBase = Mathf.Abs(currentRadius - baseRadius) < 0.5f;
        isGrounded = nearBase && !isJumping && !isReturning;
    }

    // ---------- INPUT ----------
    void HandleInput()
    {
        // sólo empezamos a cargar si estamos en el suelo
        // registrar pulsación en el buffer
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBufferTime;
        }

        // si está cargando desde el buffer y toca suelo, iniciar carga
        if (jumpBufferCounter > 0f && isGrounded && !isCharging)
        {
            charge = 0f;
            isCharging = true;
            jumpBufferCounter = 0f; // consumir buffer
        }


        // mientras mantenemos espacio y sigamos en el suelo, cargamos
        // cargar aunque esté apenas en el aire si se empezó del buffer
        if (isCharging && Input.GetKey(KeyCode.Space))
        {
            charge += chargeSpeed * Time.deltaTime;
            charge = Mathf.Clamp(charge, 0f, maxCharge);
        }


        // al soltar, si estábamos cargando, decidimos el nivel de salto
        if (isCharging && Input.GetKeyUp(KeyCode.Space))
        {
            // solo saltar si estamos en el piso
            if (isGrounded)
            {
                isCharging = false;
                DecideJumpLevel();
            }
            else
            {
                // suelta espacio en aire → cancela carga PERO no salta
                isCharging = false;
                charge = 0f;
            }
        }
    }

    // ---------- ELEGIR NIVEL DE SALTO ----------
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


    // ---------- LÓGICA DEL SALTO ----------
    void UpdateJump()
    {
        if (isJumping)
        {
            // subir suave hacia el radio objetivo
            currentRadius = Mathf.Lerp(currentRadius, targetRadius, jumpSmooth * Time.deltaTime);

            if (Mathf.Abs(currentRadius - targetRadius) < 1f)
            {
                isJumping = false;
                isReturning = true;
            }
        }
        else if (isReturning)
        {
            // bajar rápido hacia el radio base
            currentRadius = Mathf.MoveTowards(currentRadius, baseRadius, returnSpeed * Time.deltaTime);

            if (Mathf.Abs(currentRadius - baseRadius) < 0.05f)
            {
                currentRadius = baseRadius;
                isReturning = false;

                // efecto de impacto
                TriggerCameraShake();
                lastJumpLevel = 0;
            }
        }

        // actualizar posición radial
        Vector3 radialDir = (transform.position - center.position).normalized;
        transform.position = center.position + radialDir * currentRadius;
    }

    // ---------- ORIENTACIÓN (para que el squash siga el círculo) ----------
    void UpdateOrientation()
    {
        if (center == null) return;
        Vector3 radialDir = (transform.position - center.position).normalized;
        transform.up = radialDir;    // el eje Y local apunta hacia afuera del círculo azul
    }

    // ---------- SQUASH & STRETCH ----------
    void UpdateScale()
    {
        Vector3 targetScale = idleScale;

        if (isCharging && isGrounded)
        {
            targetScale = chargeScale;
        }
        else if (isJumping)
        {
            targetScale = jumpStretchScale;
        }
        else if (isReturning)
        {
            // cerca del pico o cerca del suelo
            if (Mathf.Abs(currentRadius - targetRadius) < 3f)
                targetScale = peakSquashScale;
            else if (Mathf.Abs(currentRadius - baseRadius) < 0.3f)
                targetScale = landingSquashScale;
        }

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSpeed * Time.deltaTime);
    }
    void TriggerCameraShake()
    {
        // solo temblar si es salto grande
        if (lastJumpLevel != 3)
            return;

        if (Camera.main == null) 
            return;

        CameraShake cam = Camera.main.GetComponent<CameraShake>();
        if (cam == null) 
            return;

        cam.Shake(0.10f, 2.5f); 
    }
    public void InterruptJump()
    {
        // si no está saltando, no hacer nada
        if (!isJumping) return;

        // cortar salto inmediatamente
        isJumping = false;
        isReturning = true;

        // regresar hacia el radio base
        targetRadius = baseRadius;

        // cancelar carga
        isCharging = false;
        charge = 0f;
    }

}
