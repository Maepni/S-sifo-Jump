using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRhythmManager : MonoBehaviour
{
    public static GameRhythmManager Instance { get; private set; }

    [Header("Refs")]
    public AudioSource musicSource;
    public RedJump redJump;

    [Header("Death FX")]
    public AudioSource sfxSource;          // opcional, puedes dejarlo null
    public AudioClip deathHitClip;         // pequeño golpe al morir
    public float pitchDownTarget = 0.45f;  // qué tan grave se pone la música
    public float deathFadeTime = 0.6f;     // duración del efecto
    public float restartDelay = 0.2f;      // pausa antes de recargar escena

    bool isGameOver = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PlayerDeath()
    {
        if (isGameOver) return;
        isGameOver = true;

        // bloquear controles del rojo
        if (redJump != null)
            redJump.controlsLocked = true;

        // pequeño golpe de sonido
        if (sfxSource != null && deathHitClip != null)
            sfxSource.PlayOneShot(deathHitClip);

        StartCoroutine(DeathSequence());
    }

    System.Collections.IEnumerator DeathSequence()
    {
        float t = 0f;
        float startPitch = musicSource != null ? musicSource.pitch : 1f;
        float startVolume = musicSource != null ? musicSource.volume : 1f;

        while (t < deathFadeTime)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / deathFadeTime);

            if (musicSource != null)
            {
                musicSource.pitch  = Mathf.Lerp(startPitch,  pitchDownTarget, k);
                musicSource.volume = Mathf.Lerp(startVolume, 0f,              k);
            }

            yield return null;
        }

        yield return new WaitForSecondsRealtime(restartDelay);

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }
}
