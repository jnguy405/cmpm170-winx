using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public class GameSceneManager : MonoBehaviour
{
	private const string MainMenuSceneName = "Main Menu";
	private const string MapSceneName = "Map Scene";
	private const string CreditsSceneName = "Credits Scene";

	public static GameSceneManager Instance { get; private set; }
	public static bool IsReloading { get; private set; }

	[SerializeField]
	[Tooltip("If true, loads the main menu when this object first initializes. Disable when this component also exists in gameplay scenes.")]
	private bool loadMainMenuOnAwake;

	private Coroutine reloadInputCoroutine;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
	private static void RegisterSceneLoadedCallback()
	{
		UnitySceneManager.sceneLoaded -= OnAnySceneLoaded;
		UnitySceneManager.sceneLoaded += OnAnySceneLoaded;
		OnAnySceneLoaded(UnitySceneManager.GetActiveScene(), LoadSceneMode.Single);
	}

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		if (loadMainMenuOnAwake && UnitySceneManager.GetActiveScene().name != MainMenuSceneName)
			LoadMainMenu();
	}

	private void OnEnable()
	{
		if (reloadInputCoroutine != null)
			StopCoroutine(reloadInputCoroutine);

		reloadInputCoroutine = StartCoroutine(ReloadInputLoop());
	}

	private void OnDisable()
	{
		if (reloadInputCoroutine != null)
		{
			StopCoroutine(reloadInputCoroutine);
			reloadInputCoroutine = null;
		}
	}

	private IEnumerator ReloadInputLoop()
	{
		var wait = new WaitForSecondsRealtime(0f);
		while (enabled)
		{
			if (!IsReloading && Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
				ReloadActiveScene();

			yield return wait;
		}
	}

	public static void PrepareForSceneLoad()
	{
		Time.timeScale = 1f;
		AudioListener.pause = false;
	}

	public static void ReloadActiveScene()
	{
		if (IsReloading)
			return;

		Scene activeScene = UnitySceneManager.GetActiveScene();
		if (!activeScene.IsValid() || activeScene.buildIndex < 0)
		{
			Debug.LogError("Cannot reload the active scene. Add it in File > Build Settings > Scenes In Build.");
			return;
		}

		IsReloading = true;
		PrepareForSceneLoad();

		PauseManager pauseManager = FindAnyObjectByType<PauseManager>();
		if (pauseManager != null)
			pauseManager.ClearPauseState();

		ApplyCursorForScene(activeScene.name);
		UnitySceneManager.LoadScene(activeScene.buildIndex);
	}

	private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
	{
		IsReloading = false;
		PrepareForSceneLoad();
		ApplyCursorForScene(scene.name);
	}

	private static void ApplyCursorForScene(string sceneName)
	{
		bool isMenuScene = sceneName == MainMenuSceneName || sceneName == CreditsSceneName;
		Cursor.visible = isMenuScene;
		Cursor.lockState = isMenuScene ? CursorLockMode.None : CursorLockMode.Locked;
	}

	private void LoadSceneByName(string sceneName)
	{
		if (!Application.CanStreamedLevelBeLoaded(sceneName))
		{
			Debug.LogError(
				$"Scene '{sceneName}' cannot be loaded. Add it in File > Build Settings > Scenes In Build.");
			IsReloading = false;
			return;
		}

		UnitySceneManager.LoadScene(sceneName);
	}

	public void LoadMainMenu()
	{
		PrepareForSceneLoad();
		ApplyCursorForScene(MainMenuSceneName);
		LoadSceneByName(MainMenuSceneName);
	}

	public void LoadMapScene()
	{
		PrepareForSceneLoad();
		ApplyCursorForScene(MapSceneName);
		LoadSceneByName(MapSceneName);
	}

	public void LoadCreditsScene()
	{
		PrepareForSceneLoad();
		ApplyCursorForScene(CreditsSceneName);
		LoadSceneByName(CreditsSceneName);
	}

	public void ReloadCurrentScene()
	{
		ReloadActiveScene();
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
