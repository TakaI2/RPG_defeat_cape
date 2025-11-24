using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Playing,
        GameOver
    }

    public GameState CurrentState { get; private set; }
    public float SurvivalTime { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartGame();
    }

    private void Update()
    {
        if (CurrentState == GameState.Playing)
        {
            SurvivalTime += Time.deltaTime;
        }
    }

    [SerializeField] private UIManager uiManager;

    public void StartGame()
    {
        SurvivalTime = 0f;
        CurrentState = GameState.Playing;
        Debug.Log("Game Started");
    }

    public void EndGame()
    {
        if (CurrentState == GameState.GameOver) return;

        CurrentState = GameState.GameOver;
        Debug.Log($"Game Over! Survival Time: {SurvivalTime:F2}s");
        
        if (uiManager != null)
        {
            uiManager.ShowGameOver(SurvivalTime);
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
