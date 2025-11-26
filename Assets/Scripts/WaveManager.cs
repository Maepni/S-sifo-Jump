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
    public float sensitivity = 1.3f;   
    public float minInterval = 0.35f;   
    public float recoveryTime = 0.8f;

    private float[] spectrum = new float[512];

    private float lastEnergy = 0f;
    private float lastSpawnTime = -999f;

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



    void Update()
    {
        if (!audioSource || !audioSource.isPlaying) return;

        audioSource.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);

        // 1. ENERGÍA REAL (mezcla de graves + medios)
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

        // 2. DERIVADA (los beats son aumentos, no valores altos)
        float delta = energy - lastEnergy;

        // 3. BEAT verdadero
        bool beatDetected = delta > sensitivity * lastEnergy;

        // 4. Espacios entre ondas
        float elapsed = Time.time - lastSpawnTime;
        bool enoughTime = elapsed >= Mathf.Max(minInterval, recoveryTime);

        if (beatDetected && enoughTime)
            SpawnWave(energy);

        lastEnergy = Mathf.Lerp(lastEnergy, energy, 0.5f); // filtro suave
    }

    void SpawnWave(float energy)
    {
        lastSpawnTime = Time.time;

        GameObject wave = Instantiate(sineWavePrefab);

        CircularSineWave s = wave.GetComponent<CircularSineWave>();
        s.center = center;
        s.radius = radius;

        // ==========================
        // 1) AMPLITUD EN 3 NIVELES
        // ==========================
        float amp;

        if (energy < mediumThreshold)
        {
            // energía baja -> onda pequeña
            amp = smallAmplitude;
        }
        else if (energy < largeThreshold)
        {
            // energía media -> onda mediana
            amp = mediumAmplitude;
        }
        else
        {
            // energía alta -> onda grande
            amp = largeAmplitude;
        }

        s.amplitude = amp;

        // ==========================
        // 2) VELOCIDAD DINÁMICA (igual que antes)
        // ==========================
        WaveController w = wave.GetComponent<WaveController>();
        w.sine = s;

        float energyNorm = Mathf.Clamp01(energy * bassSpeedScale);
        float dynamicSpeed = Mathf.Lerp(minSpeed, maxSpeed, energyNorm);
        w.speed = dynamicSpeed;
    }


}
