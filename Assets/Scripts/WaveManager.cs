using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Wave Settings")]
    public GameObject sineWavePrefab;
    public Transform center;
    public float radius = 300f;

    [Header("Spawn Control")]
    public float minInterval = 0.35f;   // mínimo entre ondas
    public float recoveryTime = 0.8f;   // mínimo tras un hit

    private float[] spectrum = new float[512];
    private float lastSpawnTime = -999f;
    
    [Header("Spawn Angle")]
    public float spawnAngle = 40f;   // 0 = derecha, 90 = arriba, etc.  

    [Header("Speed Settings")]
    public float minSpeed = 20f;
    public float maxSpeed = 200f;
    public float bassSpeedScale = 25f;

    [Header("Amplitude Levels")]
    public float smallAmplitude = 10f;
    public float mediumAmplitude = 40f;
    public float largeAmplitude = 80f;

    [Header("Amplitude Thresholds")]
    public float mediumThreshold = 0.015f;
    public float largeThreshold = 0.045f;

    // ---------- Beat detection con historial ----------
    [Header("Beat Detection")]
    public int historyLength = 43;         // muestras para promedio (~0.5 s)
    public float beatMultiplier = 1.4f;    // energía vs promedio
    public float minBeatEnergy = 0.002f;   // energía mínima para considerar beat
    public float minBeatDelta = 0.001f;    // cambio mínimo

    [Tooltip("Segundos desde el inicio de la pista antes de permitir ondas")]
    public float songStartDelay = 0.6f;

    [Tooltip("Cuánto debe bajar la energía para desbloquear el siguiente beat")]
    public float beatReleaseMultiplier = 1.05f;

    private float[] energyHistory;
    private int historyIndex = 0;
    private bool historyFilled = false;
    private bool beatLatched = false;

    void Start()
    {
        energyHistory = new float[historyLength];
    }

    void Update()
    {
        if (!audioSource || !audioSource.isPlaying)
            return;

        // 1) FFT
        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // 2) Energía (graves + algo de medios)
        float energy =
            spectrum[1] * 5f +
            spectrum[2] * 7f +
            spectrum[3] * 9f +
            spectrum[4] * 9f +
            spectrum[5] * 7f +
            spectrum[6] * 5f +
            spectrum[20] * 3f +
            spectrum[40] * 2f +
            spectrum[60] * 1.5f;

        // 3) Historial: si aún no hay datos, solo llenamos y salimos
        int count = historyFilled ? historyLength : historyIndex;
        if (count == 0)
        {
            energyHistory[historyIndex] = energy;
            historyIndex++;
            if (historyIndex >= historyLength)
            {
                historyIndex = 0;
                historyFilled = true;
            }
            return;
        }

        // 4) Promedio del historial (energía de contexto)
        float avg = 0f;
        for (int i = 0; i < count; i++)
            avg += energyHistory[i];
        avg /= count;

        float delta = energy - avg;

        // 5) Guardar energía actual en la cola circular
        energyHistory[historyIndex] = energy;
        historyIndex++;
        if (historyIndex >= historyLength)
        {
            historyIndex = 0;
            historyFilled = true;
        }

        // 6) No permitir beats hasta que la canción lleve un rato
        if (audioSource.time < songStartDelay)
            return;

        // 7) Condición de beat “en bruto”
        bool rawBeat =
            energy > avg * beatMultiplier &&
            energy > minBeatEnergy &&
            delta > minBeatDelta;

        float elapsed = Time.time - lastSpawnTime;
        bool enoughTime = elapsed >= Mathf.Max(minInterval, recoveryTime);

        // 8) Latch: solo un beat mientras la energía está alta
        if (!beatLatched)
        {
            if (rawBeat && enoughTime)
            {
                SpawnWave(energy);
                beatLatched = true;
            }
        }
        else
        {
            // Desbloquear cuando la energía vuelve cerca del promedio
            if (energy < avg * beatReleaseMultiplier)
            {
                beatLatched = false;
            }
        }
    }

    void SpawnWave(float energy)
    {
        if (!sineWavePrefab || !center)
            return;

        lastSpawnTime = Time.time;

        GameObject wave = Instantiate(sineWavePrefab);

        // ----- Configurar la sine wave -----
        CircularSineWave s = wave.GetComponent<CircularSineWave>();
        if (s != null)
        {
            s.center = center;
            s.radius = radius;

            // Empieza en la derecha (0°)
            s.SetStartAngle(spawnAngle);

            // Amplitud según energía
            float amp;
            if (energy < mediumThreshold)
            {
                amp = smallAmplitude;
            }
            else if (energy < largeThreshold)
            {
                amp = mediumAmplitude;
            }
            else
            {
                amp = largeAmplitude;
            }

            s.amplitude = amp;
        }

        // ----- Controlador de movimiento -----
        WaveController w = wave.GetComponent<WaveController>();
        if (w != null)
        {
            if (s != null)
                w.sine = s;

            float energyNorm = Mathf.Clamp01(energy * bassSpeedScale);
            float dynamicSpeed = Mathf.Lerp(minSpeed, maxSpeed, energyNorm);
            w.speed = dynamicSpeed;
        }
    }
}
