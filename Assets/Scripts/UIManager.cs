using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI finalScoreText;

    private void Update()
    {
        if (GameManager.Instance.CurrentState == GameManager.GameState.Playing)
        {
            float time = GameManager.Instance.SurvivalTime;
            timerText.text = $"Time: {time:F2}";
        }
    }

    public void ShowGameOver(float survivalTime)
    {
        gameOverPanel.SetActive(true);
        finalScoreText.text = $"Survived: {survivalTime:F2}s";
    }
}
