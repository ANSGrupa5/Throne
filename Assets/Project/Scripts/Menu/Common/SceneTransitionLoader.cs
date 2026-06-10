using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SceneTransitionLoader : MonoBehaviour
{
    private static SceneTransitionLoader _instance;
    private static bool _isLoading;

    private const float FadeDuration = 0.16f;
    private CanvasGroup _fadeGroup;

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
        EnsureFadeOverlay();
        yield return FadeTo(1f);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (operation != null)
        {
            operation.allowSceneActivation = false;
            while (operation.progress < 0.9f)
                yield return null;

            operation.allowSceneActivation = true;
            while (!operation.isDone)
                yield return null;
        }

        yield return null;
        yield return FadeTo(0f);
        _isLoading = false;
    }

    private void EnsureFadeOverlay()
    {
        if (_fadeGroup != null)
            return;

        GameObject canvasObject = new("SceneTransitionFade");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        _fadeGroup = canvasObject.AddComponent<CanvasGroup>();
        _fadeGroup.alpha = 0f;
        _fadeGroup.blocksRaycasts = false;
        _fadeGroup.interactable = false;

        GameObject imageObject = new("FadeImage", typeof(RectTransform));
        imageObject.transform.SetParent(canvasObject.transform, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = Color.black;

        RectTransform rect = image.transform as RectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (_fadeGroup == null)
            yield break;

        _fadeGroup.blocksRaycasts = targetAlpha > 0f;
        float startAlpha = _fadeGroup.alpha;
        if (Mathf.Approximately(startAlpha, targetAlpha))
            yield break;

        float elapsed = 0f;
        while (elapsed < FadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            _fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.Clamp01(elapsed / FadeDuration));
            yield return null;
        }

        _fadeGroup.alpha = targetAlpha;
        _fadeGroup.blocksRaycasts = targetAlpha > 0f;
    }
}
