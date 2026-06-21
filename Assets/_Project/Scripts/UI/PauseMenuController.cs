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
        AudioManager.Instance.PlaySFX(SoundEffect.ButtonClick);
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

        AudioManager.Instance.PlaySFX(SoundEffect.ButtonClick);
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
        AudioManager.Instance.PlaySFX(SoundEffect.ButtonClick);
        Time.timeScale = 1f;
        Debug.Log("[PauseMenu] Quay lại Main Menu...");
        SceneManager.LoadScene("MainMenuScene");
    }

    public void QuitGame()
    {
        AudioManager.Instance.PlaySFX(SoundEffect.ButtonClick);
        Debug.Log("[PauseMenu] Thoát game!");
        Application.Quit();
    }

    public void ChooseRedemptionEnding()
    {
        AudioManager.Instance.PlaySFX(SoundEffect.ButtonClick);
        Time.timeScale = 1f;
        PlayerPrefs.SetString("PendingCutscene", "Redemption");
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenuScene");
    }

    public void ChooseLegacyEnding()
    {
        AudioManager.Instance.PlaySFX(SoundEffect.ButtonClick);
        Time.timeScale = 1f;
        PlayerPrefs.SetString("PendingCutscene", "Legacy");
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainMenuScene");
    }

    public bool IsPaused()
    {
        return isPaused;
    }
}
