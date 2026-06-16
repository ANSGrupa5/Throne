using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private Transform resultsRoot;
    [SerializeField] private GameOverResultRow resultRowPrefab;

    [Header("Navigation")]
    [SerializeField] private GameOverNavigationConfig navigationConfig;

    private readonly List<GameOverResultRow> spawnedRows = new List<GameOverResultRow>();

    private void Awake()
    {
        Time.timeScale = 1f;
        NormalizeCanvasForRuntime();
    }

    private void Start()
    {
        RefreshView();
    }

    public void RefreshView()
    {
        ApplyReasonText();
        ApplyResults();
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("GameOverController cannot load scene because the requested scene name is empty.");
            return;
        }

        CleanupPayload();
        SceneManager.LoadScene(sceneName);
    }

    public void ReturnToMainMenu()
    {
        LoadConfiguredScene(navigationConfig != null ? navigationConfig.MainMenuSceneName : string.Empty, "main menu");
    }

    public void ReturnToLobby()
    {
        LoadConfiguredScene(navigationConfig != null ? navigationConfig.SingleplayerLobbySceneName : string.Empty, "singleplayer lobby");
    }

    private void LoadConfiguredScene(string sceneName, string sceneRole)
    {
        if (navigationConfig == null)
        {
            Debug.LogError($"GameOverController cannot load {sceneRole} because GameOverNavigationConfig is not assigned.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"GameOverController cannot load {sceneRole} because its scene reference is empty in GameOverNavigationConfig.");
            return;
        }

        LoadScene(sceneName);
    }

    private void ApplyReasonText()
    {
        if (reasonText != null)
            reasonText.text = GameOverPayload.GetReasonText();
    }

    private void ApplyResults()
    {
        List<GameOverPayload.MatchResult> orderedResults = GameOverPayload.results
            .Where(result => result != null)
            .OrderByDescending(result => result.kills)
            .ThenBy(result => result.deaths)
            .ThenBy(result => result.displayName)
            .ToList();

        if (StatsManager.Instance != null)
            StatsManager.Instance.CheckIfPlayerWon(orderedResults);
        else
            Debug.LogWarning("GameOverController could not update win/loss stats because StatsManager.Instance is null.");

        ApplyRowResults(orderedResults);
    }

    private void ApplyRowResults(List<GameOverPayload.MatchResult> results)
    {
        if (resultsRoot == null)
        {
            Debug.LogError("GameOverController cannot display match results because resultsRoot is not assigned.");
            return;
        }

        if (resultRowPrefab == null)
        {
            Debug.LogError("GameOverController cannot display match results because resultRowPrefab is not assigned.");
            return;
        }

        GameObject rowTemplate = resultRowPrefab.gameObject;
        rowTemplate.SetActive(false);

        for (int i = spawnedRows.Count - 1; i >= 0; i--)
        {
            GameOverResultRow row = spawnedRows[i];
            if (row != null)
                Destroy(row.gameObject);
        }
        spawnedRows.Clear();

        if (results.Count == 0)
        {
            Debug.LogWarning("GameOverController has no match results to display.");
            return;
        }

        int rowIndex = 0;

        GameOverResultRow headerRow = CreateResultRow();
        PositionRow(headerRow.transform as RectTransform, rowIndex);
        headerRow.BindHeader();
        rowIndex++;

        for (int i = 0; i < results.Count; i++)
        {
            GameOverPayload.MatchResult result = results[i];
            if (result == null)
                continue;

            GameOverResultRow row = CreateResultRow();
            PositionRow(row.transform as RectTransform, rowIndex);
            row.Bind(i + 1, result, i == 0);
            rowIndex++;
        }
    }

    private GameOverResultRow CreateResultRow()
    {
        GameOverResultRow row = Instantiate(resultRowPrefab, resultsRoot);
        row.gameObject.SetActive(true);
        row.enabled = true;
        row.transform.SetAsLastSibling();
        spawnedRows.Add(row);

        RectTransform rect = row.transform as RectTransform;
        if (rect != null && rect.localScale == Vector3.zero)
            rect.localScale = Vector3.one;

        return row;
    }

    private void NormalizeCanvasForRuntime()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            return;

        Transform canvasTransform = canvas.transform;
        Vector3 scale = canvasTransform.localScale;
        if (Mathf.Approximately(scale.x, 0f) || Mathf.Approximately(scale.y, 0f) || Mathf.Approximately(scale.z, 0f))
        {
            canvasTransform.localScale = Vector3.one;
            Debug.LogWarning("GameOverController corrected a zero-scale Canvas at runtime. Fix the GameOver Canvas RectTransform scale to 1,1,1 in the Unity scene.");
        }

        if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Debug.LogWarning("GameOverController changed a Screen Space Camera Canvas with no camera to Screen Space Overlay at runtime. Fix the GameOver Canvas render mode or camera assignment in the Unity scene.");
        }
    }

    private void PositionRow(RectTransform rect, int index)
    {
        if (rect == null) return;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -30f - (50f * index));
    }

    private void CleanupPayload()
    {
        GameOverPayload.Clear();
        GameSessionBootstrap.ClearSession();
    }
}


