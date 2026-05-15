using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

/// <summary>
/// Scene-local UI hooks that call the persistent GameSceneManager singleton.
/// Wire menu buttons to this component so clicks still work after GameSceneManager moves to DontDestroyOnLoad.
/// </summary>
public class MenuUI : MonoBehaviour
{
	public void LoadMainMenu()
	{
		if (GameSceneManager.Instance != null)
			GameSceneManager.Instance.LoadMainMenu();
		else
			UnitySceneManager.LoadScene("Main Menu");
	}

	public void LoadMapScene()
	{
		if (GameSceneManager.Instance != null)
			GameSceneManager.Instance.LoadMapScene();
		else
			UnitySceneManager.LoadScene("Map Scene");
	}

	public void LoadCreditsScene()
	{
		if (GameSceneManager.Instance != null)
			GameSceneManager.Instance.LoadCreditsScene();
		else
			UnitySceneManager.LoadScene("Credits Scene");
	}

	public void QuitGame()
	{
		if (GameSceneManager.Instance != null)
			GameSceneManager.Instance.QuitGame();
		else
			Application.Quit();
	}
}
