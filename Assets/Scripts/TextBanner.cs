using UnityEngine;
using TMPro;
using System.Collections;

public class TextBanner : MonoBehaviour
{
    public RectTransform banner;
    public Vector2 offScreenRight = new Vector2(700f, 850f);
    public Vector2 onScreen = new Vector2(0f, 850f);

    public float moveDuration = 0.8f;
    public float stayDuration = 3f;
    public float interval = 12f;  // cada cuánto aparece el cartel

    void Start()
    {
        banner.anchoredPosition = offScreenRight;
        StartCoroutine(BannerRoutine());
    }

    IEnumerator BannerRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            yield return StartCoroutine(Move(banner, offScreenRight, onScreen, moveDuration));
            yield return new WaitForSeconds(stayDuration);
            yield return StartCoroutine(Move(banner, onScreen, offScreenRight, moveDuration));
        }
    }

    IEnumerator Move(RectTransform target, Vector2 start, Vector2 end, float time)
    {
        float t = 0f;

        while (t < time)
        {
            t += Time.deltaTime;
            float lerp = Mathf.SmoothStep(0, 1, t / time);
            target.anchoredPosition = Vector2.Lerp(start, end, lerp);
            yield return null;
        }

        target.anchoredPosition = end;
    }
}
