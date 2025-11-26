using UnityEngine;
using TMPro;

public class ScoreUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text scoreText;
    public TMP_Text comboText;
    public TMP_Text multiplierText;
    public TMP_Text riskText;

    void Update()
    {
        var sm = ScoreManager.Instance;
        if (sm == null) return;

        // Score
        scoreText.text = $"Score: {sm.score}";

        // Combo
        comboText.text = $"Combo: {sm.combo}";

        // Multiplicador
        multiplierText.text = $"x{GetMultiplier(sm.combo)}";

        // Riesgo
        riskText.text = $"Risk: {Mathf.RoundToInt(sm.riskCurrent)}";
    }

    int GetMultiplier(int combo)
    {
        var sm = ScoreManager.Instance;

        if (combo >= sm.comboForX4) return 4;
        if (combo >= sm.comboForX3) return 3;
        if (combo >= sm.comboForX2) return 2;
        return 1;
    }
}
