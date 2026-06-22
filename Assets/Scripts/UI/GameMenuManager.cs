using UnityEngine;
// Обязательно добавляем эту строчку для новой системы ввода!
using UnityEngine.InputSystem;

public class GameMenuManager : MonoBehaviour
{
    public static bool IsGamePaused = false;

    [Header("Экраны меню")]
    public GameObject mainMenuPanel;
    public GameObject pauseMenuPanel;

    private bool isInMainMenu = true;

    void Start()
    {
        ShowMainMenu();
    }

    void Update()
    {
        if (isInMainMenu) return;

        // НОВЫЙ СПОСОБ: Проверяем нажатие клавиши Escape через новый Input System
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (IsGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    // --- 1. ГЛАВНОЕ МЕНЮ ---
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        pauseMenuPanel.SetActive(false);

        Time.timeScale = 0f;
        isInMainMenu = true;
        IsGamePaused = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void StartGame()
    {
        mainMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        isInMainMenu = false;
        IsGamePaused = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // --- 2. МЕНЮ ПАУЗЫ ---
    public void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f;
        IsGamePaused = true;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        IsGamePaused = false;

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // --- 3. ВЫХОД ИЗ ИГРЫ ---
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Игра полностью закрыта!");
    }
}
