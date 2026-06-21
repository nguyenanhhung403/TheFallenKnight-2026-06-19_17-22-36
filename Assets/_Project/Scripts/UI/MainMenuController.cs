using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý các sự kiện click nút của màn hình Main Menu.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public void PlayGame()
    {
        Debug.Log("[MainMenu] Bắt đầu chơi game, tải scene 'SampleScene'...");
        SceneManager.LoadScene("SampleScene");
    }

    public void QuitGame()
    {
        Debug.Log("[MainMenu] Thoát game!");
        Application.Quit();
    }
}
