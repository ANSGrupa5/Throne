using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneTransitionLoader : MonoBehaviour
{
    private static SceneTransitionLoader _instance;
    private static bool _isLoading;

    public static bool IsLoading => _isLoading;

    public static void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        EnsureInstance();

        if (_isLoading)
            return;

        _instance.StartCoroutine(_instance.LoadSceneRoutine(sceneName.Trim()));
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
            return;

        GameObject loaderObject = new(nameof(SceneTransitionLoader));
        DontDestroyOnLoad(loaderObject);
        _instance = loaderObject.AddComponent<SceneTransitionLoader>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        _isLoading = true;
        yield return null;

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (operation != null)
        {
            operation.allowSceneActivation = true;
            while (!operation.isDone)
                yield return null;
        }

        _isLoading = false;
    }
}
