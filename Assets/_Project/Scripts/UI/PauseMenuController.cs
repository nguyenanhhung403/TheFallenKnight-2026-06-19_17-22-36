using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý trạng thái Tạm dừng (Pause) của Game: Dừng thời gian (TimeScale = 0),
/// hiển thị panel tùy chọn và cho phép tiếp tục, quay về Main Menu hoặc Thoát.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [Header("--- Cấu hình UI ---")]
    public GameObject pausePanel;

    private bool isPaused = false;

    private void Awake()
    {
        Instance = this;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    void Update()
    {
        // Nhấn nút ESC để Tạm dừng hoặc Tiếp tục game
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
        Time.timeScale = 1f;
        Debug.Log("[PauseMenu] Tiếp tục game.");
    }

    public void PauseGame()
    {
        // Không cho phép Pause nếu người chơi đã chết (Màn hình Game Over đang hiển thị)
        PlayerController player = Object.FindAnyObjectByType<PlayerController>();
        if (player != null && player.IsDead()) return;

        isPaused = true;
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
        Time.timeScale = 0f;
        Debug.Log("[PauseMenu] Tạm dừng game.");
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        Debug.Log("[PauseMenu] Quay lại Main Menu...");
        SceneManager.LoadScene("MainMenuScene");
    }

    public void QuitGame()
    {
        Debug.Log("[PauseMenu] Thoát game!");
        Application.Quit();
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}
