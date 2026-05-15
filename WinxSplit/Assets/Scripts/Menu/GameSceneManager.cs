using UnityEngine;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class GameSceneManager : MonoBehaviour
{
	private string mainMenuSceneName = "Main Menu";
	private string mapSceneName = "Map Scene";
	private string creditsSceneName = "Credits Scene";

	public static GameSceneManager Instance { get; private set; }

	[SerializeField]
	[Tooltip("If true, loads the main menu when this object first initializes. Disable when this component also exists in gameplay scenes.")]
	private bool loadMainMenuOnAwake = true;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		if (loadMainMenuOnAwake && UnitySceneManager.GetActiveScene().name != mainMenuSceneName)
			LoadMainMenu();
	}

	private void LoadSceneByName(string sceneName)
	{
		if (!Application.CanStreamedLevelBeLoaded(sceneName))
		{
			Debug.LogError(
				$"Scene '{sceneName}' cannot be loaded. Add it in File > Build Settings > Scenes In Build.");
			return;
		}

		UnitySceneManager.LoadScene(sceneName);
	}

	public void LoadMainMenu()
	{
		Time.timeScale = 1f;
		AudioListener.pause = false;
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
		LoadSceneByName(mainMenuSceneName);
	}

	public void LoadMapScene()
	{
		Time.timeScale = 1f;
		AudioListener.pause = false;
		LoadSceneByName(mapSceneName);
	}

	public void LoadCreditsScene()
	{
		Time.timeScale = 1f;
		AudioListener.pause = false;
		LoadSceneByName(creditsSceneName);
	}

	public void ReloadCurrentScene()
	{
		Time.timeScale = 1f;
		AudioListener.pause = false;
		LoadSceneByName(UnitySceneManager.GetActiveScene().name);
	}

	public void QuitGame()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}

	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}
}
