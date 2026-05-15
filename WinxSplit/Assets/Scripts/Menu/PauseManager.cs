using UnityEngine;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuCanvas;
    
    private bool isPaused = false;

    void Start()
    {
        if (pauseMenuCanvas != null)
        {
            pauseMenuCanvas.SetActive(false);
        }
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!isPaused)
            {
                PauseGame();
            }
        }
    }

    private void PauseGame()
    {
        isPaused = true;
        pauseMenuCanvas.SetActive(true);
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        
        // Pause all audio
        AudioListener.pause = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pauseMenuCanvas.SetActive(false);
        Time.timeScale = 1f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        
        // Resume all audio
        AudioListener.pause = false;
    }

    public void OnVolumeButtonPressed()
    {
        // Volume control will be implemented later
    }
}