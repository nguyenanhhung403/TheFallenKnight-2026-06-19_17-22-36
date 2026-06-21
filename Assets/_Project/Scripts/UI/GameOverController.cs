using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý màn hình báo tử (Game Over) khi người chơi hết máu.
/// </summary>
public class GameOverController : MonoBehaviour
{
    public static GameOverController Instance { get; private set; }

    [Header("--- Cấu hình UI ---")]
    public GameObject gameOverPanel;

    private void Awake()
    {
        Instance = this;
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    public void TriggerGameOver()
    {
        Debug.Log("[GameOver] Người chơi đã hy sinh. Hiển thị màn hình Game Over.");
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameOver] Chơi lại màn chơi...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        Debug.Log("[GameOver] Quay lại Main Menu...");
        SceneManager.LoadScene("MainMenuScene");
    }
}
