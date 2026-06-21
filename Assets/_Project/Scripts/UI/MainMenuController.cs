using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý các sự kiện click nút của màn hình Main Menu.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("--- UI Panels ---")]
    public GameObject mainMenuContainer;
    public GameObject storySubmenuPanel;

    public void PlayGame()
    {
        AudioManager.Instance.PlaySFX(SoundEffect.ButtonClick);
        Debug.Log("[MainMenu] Bắt đầu chơi game, phát intro cutscene...");
        StoryCutsceneController story = FindAnyObjectByType<StoryCutsceneController>();
        if (story != null)
        {
            story.PlayGameWithIntro();
        }
        else
        {
            Debug.LogWarning("[MainMenu] Không tìm thấy StoryCutsceneController, tải trực tiếp 'SampleScene'...");
            SceneManager.LoadScene("SampleScene");
        }
    }

    public void QuitGame()
    {
        AudioManager.Instance.PlaySFX(SoundEffect.ButtonClick);
        Debug.Log("[MainMenu] Thoát game!");
        Application.Quit();
    }

    public void ShowStorySubmenu()
    {
        AudioManager.Instance.PlaySFX(SoundEffect.ButtonClick);
        if (mainMenuContainer != null) mainMenuContainer.SetActive(false);
        if (storySubmenuPanel != null) storySubmenuPanel.SetActive(true);
    }

    public void HideStorySubmenu()
    {
        AudioManager.Instance.PlaySFX(SoundEffect.ButtonClick);
        if (mainMenuContainer != null) mainMenuContainer.SetActive(true);
        if (storySubmenuPanel != null) storySubmenuPanel.SetActive(false);
    }
}
