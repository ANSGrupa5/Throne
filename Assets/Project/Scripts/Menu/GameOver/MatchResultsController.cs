using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MatchResultsController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text fallbackResultsText;
    [SerializeField] private Transform resultsRoot;
    [SerializeField] private MatchResultRow resultRowPrefab;

    [Header("Styling")]
    [SerializeField] private Color winnerColor = new Color(1f, 0.86f, 0.25f, 1f);
    [SerializeField] private Color fallbackTextColor = Color.white;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string lobbySceneName = LobbyLaunchContext.SharedLobbySceneName;

    private readonly List<MatchResultRow> _generatedRows = new();
    private bool _hasAppliedWinLossStats;

    private void Awake()
    {
        Time.timeScale = 1f;
    }

    private void Start()
    {
        RefreshView();
    }

    public void RefreshView()
    {
        ApplyResults();
    }

    public void LoadScene(string sceneName)
    {
        CleanupPayload();
        SceneTransitionLoader.LoadScene(sceneName);
    }

    public void ReturnToMainMenu()
    {
        LoadScene(mainMenuSceneName);
    }

    public void ReturnToLobby()
    {
        if (ShouldReturnToMultiplayerLobby())
            LobbyLaunchContext.RequestMultiplayer();
        else
            LobbyLaunchContext.RequestSingleplayer();

        LoadScene(lobbySceneName);
    }

    private bool ShouldReturnToMultiplayerLobby()
    {
        if (GameSessionBootstrap.CurrentSession != null)
            return !GameSessionBootstrap.CurrentSession.isSingleplayer;

        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        return bootstrap != null && (bootstrap.IsServerStarted || bootstrap.IsClientStarted);
    }

    private void ApplyResults()
    {
        List<GameOverPayload.MatchResult> orderedResults = GameOverPayload.results
            .Where(result => result != null)
            .OrderByDescending(result => result.kills)
            .ThenBy(result => result.deaths)
            .ThenBy(result => result.displayName)
            .ToList();

        if (!_hasAppliedWinLossStats && PlayerProfileStats.Instance != null)
        {
            PlayerProfileStats.Instance.CheckIfPlayerWon(orderedResults);
            _hasAppliedWinLossStats = true;
        }

        ApplyFallbackResultsText(orderedResults);
        ApplyRowResults(orderedResults);
    }

    private void ApplyFallbackResultsText(List<GameOverPayload.MatchResult> results)
    {
        if (fallbackResultsText == null)
            return;

        bool hasDedicatedRows = resultsRoot != null && resultRowPrefab != null;
        bool shouldUseFallback = !hasDedicatedRows || results.Count == 0;
        fallbackResultsText.gameObject.SetActive(shouldUseFallback);
        if (!shouldUseFallback)
            return;

        fallbackResultsText.color = fallbackTextColor;

        if (results.Count == 0)
        {
            fallbackResultsText.gameObject.SetActive(true);
            fallbackResultsText.text = "No match data.";
            return;
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < results.Count; i++)
        {
            GameOverPayload.MatchResult result = results[i];
            if (result == null)
                continue;

            builder.AppendLine(FormatRowLine(i + 1, result));
        }

        fallbackResultsText.gameObject.SetActive(true);
        fallbackResultsText.text = builder.ToString().TrimEnd();
    }

    private void ApplyRowResults(List<GameOverPayload.MatchResult> results)
    {
        if (resultsRoot == null || resultRowPrefab == null)
            return;

        ClearGeneratedRows();
        if (resultRowPrefab.gameObject.scene.IsValid())
            resultRowPrefab.gameObject.SetActive(false);

        if (results.Count == 0)
            return;

        int rowIndex = 0;

        MatchResultRow headerRow = CreateGeneratedRow();
        PositionRow(headerRow.transform as RectTransform, rowIndex);
        headerRow.BindHeader();
        rowIndex++;

        for (int i = 0; i < results.Count; i++)
        {
            GameOverPayload.MatchResult result = results[i];
            if (result == null)
                continue;

            MatchResultRow row = CreateGeneratedRow();
            PositionRow(row.transform as RectTransform, rowIndex);
            row.Bind(i + 1, result, i == 0 ? winnerColor : Color.white, i % 2 == 1);
            rowIndex++;
        }
    }

    private MatchResultRow CreateGeneratedRow()
    {
        MatchResultRow row = Instantiate(resultRowPrefab, resultsRoot);
        row.gameObject.SetActive(true);
        _generatedRows.Add(row);
        return row;
    }

    private void ClearGeneratedRows()
    {
        for (int i = _generatedRows.Count - 1; i >= 0; i--)
        {
            MatchResultRow row = _generatedRows[i];
            if (row != null)
                Destroy(row.gameObject);
        }

        _generatedRows.Clear();
    }

    private void PositionRow(RectTransform rect, int index)
    {
        if (rect == null) return;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -36f - (66f * index));
    }

    private string TranslateReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return "Match ended.";

        return reason switch
        {
            "TimeUp" => "Time up.",
            "LastAlive" => "Last alive.",
            "Manual" => "Ended manually.",
            _ => reason
        };
    }

    private string FormatRowLine(int rank, GameOverPayload.MatchResult result)
    {
        string name = string.IsNullOrWhiteSpace(result.displayName) ? "Unknown" : result.displayName;
        return $"{rank}. {name}   K:{result.kills}   D:{result.deaths}";
    }

    private void CleanupPayload()
    {
        GameOverPayload.Clear();
        GameSessionBootstrap.ClearSession();
    }
}


