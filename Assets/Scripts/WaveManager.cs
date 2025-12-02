using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Wave Settings")]
    public GameObject sineWavePrefab;
    public Transform center;
    public float radius = 300f;

    [Header("Spawn Angle")]
    public float spawnAngle = 55f;   // 0 = derecha, 90 = arriba, etc.

    [Header("Spawn Control (tiempo)")]
    public float minInterval = 0.25f;   // tiempo mínimo real entre ondas
    public float recoveryTime = 0.5f;   // puedes subirlo si siguen muy juntas
    public float songStartDelay = 0.5f; // segundos antes de empezar a lanzar ondas

    [Header("Amplitude Levels")]
    public float smallAmplitude = 10f;
    public float mediumAmplitude = 40f;
    public float largeAmplitude = 80f;

    [Header("Speed Settings")]
    public float baseSpeed = 80f;
    public float speedEnergyMin = 0.8f;   // multiplicador de velocidad para energía 0
    public float speedEnergyMax = 1.3f;   // multiplicador para energía 1

    [Header("Beat Map JSON")]
    public TextAsset beatJson;   // arrastra beat_data.json aquí

    [Header("Beat Filtering")]
    [Range(0f, 1f)]
    public float minEnergyToSpawn = 0.18f; // beats más débiles se ignoran
    [Tooltip("1 = puede haber onda en cada beat; 2 = como máximo 1 cada 2 beats, etc.")]
    public int minBeatsBetweenSpawns = 1;

    private BeatMap beatMap;
    private int beatIndex = 0;
    private int lastSpawnBeatIndex = -999;

    private float lastSpawnTime = -999f;

    void Awake()
    {
        if (beatJson != null)
        {
            beatMap = JsonUtility.FromJson<BeatMap>(beatJson.text);
        }
        else
        {
            Debug.LogError("WaveManager: falta beatJson (beat_data.json).");
        }
    }

    void Update()
    {
        if (beatMap == null || beatMap.beats == null || beatMap.beats.Length == 0)
            return;

        if (!audioSource || !audioSource.isPlaying)
            return;

        float t = audioSource.time;
        if (t < songStartDelay)
            return;

        float elapsed = Time.time - lastSpawnTime;
        float minGap = Mathf.Max(minInterval, recoveryTime);
        bool enoughTime = elapsed >= minGap;

        // Recorremos beats que ya pasaron en el tiempo de la canción
        while (beatIndex < beatMap.beats.Length &&
               t >= beatMap.beats[beatIndex].time)
        {
            float energy = beatMap.beats[beatIndex].energy;

            // ¿Cuántos beats han pasado desde la última onda?
            int beatsSinceLast = beatIndex - lastSpawnBeatIndex;

            // Condición para spawnear:
            //  - energía por encima del umbral
            //  - ha pasado el mínimo de beats desde la última
            //  - también se respeta un gap de tiempo real
            if (energy >= minEnergyToSpawn &&
                beatsSinceLast >= minBeatsBetweenSpawns &&
                enoughTime)
            {
                SpawnWaveFromEnergy(energy);
                lastSpawnBeatIndex = beatIndex;
                lastSpawnTime = Time.time;

                // hasta que pase minGap, no consideramos más spawns
                enoughTime = false;
            }

            // Aunque no spawneemos, avanzamos al siguiente beat
            beatIndex++;
        }
    }

    void SpawnWaveFromEnergy(float energy)
    {
        if (!sineWavePrefab || !center)
            return;

        GameObject wave = Instantiate(sineWavePrefab);

        // ----- Geometría de la onda -----
        CircularSineWave s = wave.GetComponent<CircularSineWave>();
        if (s != null)
        {
            s.center = center;
            s.radius = radius;
            s.SetStartAngle(spawnAngle);

            // Tamaño en tres niveles claros
            float amp;
            if (energy < 0.33f)
                amp = smallAmplitude;
            else if (energy < 0.66f)
                amp = mediumAmplitude;
            else
                amp = largeAmplitude;

            s.amplitude = amp;
        }

        // ----- Movimiento -----
        WaveController w = wave.GetComponent<WaveController>();
        if (w != null)
        {
            if (s != null)
                w.sine = s;

            float speedMul = Mathf.Lerp(speedEnergyMin, speedEnergyMax, energy);
            w.speed = baseSpeed * speedMul;
        }
    }
}
