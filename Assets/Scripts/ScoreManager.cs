using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("Score Values")]
    public int score = 0;

    [Header("Combo System")]
    public int combo = 0;
    public int maxCombo = 0;
    public int wavesEvaded = 0;

    [Header("Multipliers")]
    public int comboForX2 = 3;
    public int comboForX3 = 6;
    public int comboForX4 = 10;

    [Header("Wave Points")]
    public int baseWavePoints = 10;

    [Header("Risk System")]
    public float riskRadius = 0.6f;
    public float riskGainPerSecond = 10f;
    public float riskCurrent = 0f;

    [Header("References")]
    public Transform player; // círculo rojo

    // Ondas activas actualmente en escena
    private readonly List<Transform> activeWaves = new List<Transform>();


    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    void Update()
    {
        UpdateRisk();
    }


    // ===================================================
    // RISK SYSTEM BASADO EN TU FORMA REAL DEL JUEGO
    // ===================================================
    void UpdateRisk()
    {
        if (player == null || activeWaves.Count == 0)
            return;

        float minDist = float.MaxValue;

        foreach (var w in activeWaves)
        {
            if (w == null) continue;

            float d = Vector3.Distance(player.position, w.position);
            if (d < minDist)
                minDist = d;
        }

        if (minDist <= riskRadius)
        {
            float factor = 1f - (minDist / riskRadius);
            riskCurrent += riskGainPerSecond * factor * Time.deltaTime;
        }
    }


    // ===================================================
    // CALCULAR MULTIPLICADOR
    // ===================================================
    int GetMultiplier()
    {
        if (combo >= comboForX4) return 4;
        if (combo >= comboForX3) return 3;
        if (combo >= comboForX2) return 2;
        return 1;
    }


    // ===================================================
    // ONDA ESQUIVADA
    // ===================================================
    public void RegisterWaveEvaded()
    {
        wavesEvaded++;
        combo++;

        if (combo > maxCombo)
            maxCombo = combo;

        int m = GetMultiplier();
        int waveScore = baseWavePoints * m;
        int riskScore = Mathf.RoundToInt(riskCurrent * m);

        score += waveScore + riskScore;

        riskCurrent = 0f;

        Debug.Log($"[WAVE EVADED] Combo: {combo}  Added: {waveScore + riskScore} Total: {score}");
    }


    // ===================================================
    // ONDA GOLPEA AL JUGADOR
    // ===================================================
    public void RegisterWaveHit()
    {
        combo = 0;
        riskCurrent = 0f;

        Debug.Log("[WAVE HIT] Combo reset.");
    }


    // ===================================================
    // MUERTE POR SIERRA
    // ===================================================
    public void RegisterSawDeath()
    {
        Debug.Log($"[SAW DEATH] FINAL SCORE: {score}, MaxCombo: {maxCombo}, WavesEvaded: {wavesEvaded}");
    }


    // ===================================================
    // REGISTRAR Y ELIMINAR ONDAS ACTIVAS
    // ===================================================
    public void RegisterWave(Transform w)
    {
        if (!activeWaves.Contains(w))
            activeWaves.Add(w);
    }

    public void UnregisterWave(Transform w)
    {
        if (activeWaves.Contains(w))
            activeWaves.Remove(w);
    }
}
