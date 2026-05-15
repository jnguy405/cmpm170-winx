using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private PlayerController playerController;

    private bool isPaused;

    private void Start()
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        StartCoroutine(PauseInputLoop());
    }

    private IEnumerator PauseInputLoop()
    {
        var wait = new WaitForSecondsRealtime(0f);
        while (enabled)
        {
            if (isPaused)
                InputSystem.Update();

            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (isPaused)
                    ResumeGame();
                else
                    PauseGame();
            }

            yield return wait;
        }
    }

    public void PauseGame()
    {
        if (isPaused || pauseMenuCanvas == null)
            return;

        isPaused = true;
        pauseMenuCanvas.SetActive(true);
        Time.timeScale = 0f;
        SetPlayerPaused(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        AudioListener.pause = true;
    }

    public void ResumeGame()
    {
        if (!isPaused || pauseMenuCanvas == null)
            return;

        isPaused = false;
        pauseMenuCanvas.SetActive(false);
        Time.timeScale = 1f;
        SetPlayerPaused(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        AudioListener.pause = false;
    }

    private void SetPlayerPaused(bool paused)
    {
        if (playerController == null)
            playerController = FindAnyObjectByType<PlayerController>();

        if (playerController != null)
            playerController.SetGamePaused(paused);
    }

    public void OnVolumeButtonPressed()
    {
        // Volume control will be implemented later
    }

    public void LoadMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SetPlayerPaused(false);

        if (pauseMenuCanvas != null)
            pauseMenuCanvas.SetActive(false);

        if (GameSceneManager.Instance != null)
            GameSceneManager.Instance.LoadMainMenu();
    }
}