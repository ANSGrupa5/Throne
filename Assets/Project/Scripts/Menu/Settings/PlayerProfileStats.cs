using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerProfileStats : MonoBehaviour
{
    public static PlayerProfileStats Instance;

    [SerializeField] private TextMeshProUGUI OppsElimText;
    [SerializeField] private TextMeshProUGUI TimesElimText;
    [SerializeField] private TextMeshProUGUI PowerUpsPickedUpText;
    [SerializeField] private TextMeshProUGUI WinsText;
    [SerializeField] private TextMeshProUGUI LossesText;
    [SerializeField] private TextMeshProUGUI DistDrivenText;
    [SerializeField] private TextMeshProUGUI MatchesCountText;
    [SerializeField] private TextMeshProUGUI WinPercentageText;

    private GameObject statsScreen;
    private GameObject player = null;
    private string playerName = null;

    private int OppsElim, TimesElim, PowerUpsPickedUp, Wins, Losses;
    private float DistDriven;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadStatValues();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GetPlayerPrefab(string playername)
    {
        GameObject playerObject = GameObject.Find("motorFINAL2_WORKING(Clone)");
        if (playerObject == null)
            playerObject = GameObject.Find("motor22(Clone)");

        SetPlayer(playerObject, playername);
    }

    public void SetPlayer(GameObject playerObject, string playername)
    {
        player = playerObject;
        playerName = playername;
    }

    public bool CheckIfPlayerIsKiller(GameObject objectToCheck)
    {
        if (player == objectToCheck)
            return true;
        return false;
    }

    public bool CheckIfPlayerIsEliminated(VehicleLife objectToCheck)
    {
        if (objectToCheck == null)
            return false;

        if (player == objectToCheck.gameObject)
            return true;
        return false;
    }

    public bool CheckIfPlayerPickedUpPowerUp(VehicleLife objectToCheck)
    {
        if (objectToCheck == null)
            return false;

        if (player == objectToCheck.gameObject && playerName == objectToCheck.DisplayName)
            return true;
        return false;
    }

    public void CheckIfPlayerWon(List<GameOverPayload.MatchResult> results)
    {
        if (results == null || results.Count == 0)
            return;

        GameOverPayload.MatchResult winner = results[0];

        if (winner.displayName == playerName)
            IncWins();
        else
            IncLosses();
    }

    public void LoadStats()
    {
        LoadStatValues();
        TryBindStatsScreen();
        RefreshStatsUi();
    }

    private void LoadStatValues()
    {
        OppsElim = PlayerPrefs.GetInt("StatOppsElim", 0);
        TimesElim = PlayerPrefs.GetInt("StatTimesElim", 0);
        PowerUpsPickedUp = PlayerPrefs.GetInt("StatPowerUpsPickedUp", 0);
        Wins = PlayerPrefs.GetInt("StatWins", 0);
        Losses = PlayerPrefs.GetInt("StatLosses", 0);
        DistDriven = PlayerPrefs.GetFloat("StatDistDriven", 0f);
    }

    private void RefreshStatsUi()
    {
        if (OppsElimText != null)
            OppsElimText.text = OppsElim.ToString();
        if (TimesElimText != null)
            TimesElimText.text = TimesElim.ToString();
        if (PowerUpsPickedUpText != null)
            PowerUpsPickedUpText.text = PowerUpsPickedUp.ToString();
        if (WinsText != null)
            WinsText.text = Wins.ToString();
        if (LossesText != null)
            LossesText.text = Losses.ToString();
        if (DistDrivenText != null)
            DistDrivenText.text = $"{DistDriven:F2} km";

        int matches = Wins + Losses;
        if (MatchesCountText != null)
            MatchesCountText.text = matches.ToString();
        if (WinPercentageText != null)
            WinPercentageText.text = matches > 0 ? $"{(Wins * 100f / matches):0}%" : "0%";
    }

    private void TryBindStatsScreen()
    {
        if (HasStatsTextReferences())
            return;

        if (statsScreen == null)
            statsScreen = FindStatsScreen();
        if (statsScreen == null)
            return;

        BindTextIfMissing(ref OppsElimText, "StatsPanel/StatsGrid/StatOppsElimCard/StatOppsElimValueText", "StatOppsElimValueText");
        BindTextIfMissing(ref TimesElimText, "StatsPanel/StatsGrid/StatTimesElimCard/StatTimesElimValueText", "StatTimesElimValueText");
        BindTextIfMissing(ref PowerUpsPickedUpText, "StatsPanel/StatsGrid/StatTotalPowerUpsCard/StatTotalPowValueText", "StatTotalPowValueText");
        BindTextIfMissing(ref WinsText, "StatsPanel/StatsGrid/StatWinsCard/StatWinsValueText", "StatWinsValueText");
        BindTextIfMissing(ref LossesText, "StatsPanel/StatsGrid/StatLossesCard/StatLossesValueText", "StatLossesValueText");
        BindTextIfMissing(ref DistDrivenText, "StatsPanel/StatsGrid/StatDistanceDrivenCard/StatDistDrivenValueText", "StatDistDrivenValueText");
        BindTextIfMissing(ref MatchesCountText, "StatsPanel/SummaryRow/MatchesCountCard/MatchesCountValueText", "MatchesCountValueText");
        BindTextIfMissing(ref WinPercentageText, "StatsPanel/SummaryRow/WinsPercentageCard/WinsPercentageValueText", "WinsPercentageValueText");
    }

    private bool HasStatsTextReferences()
    {
        return OppsElimText != null &&
               TimesElimText != null &&
               PowerUpsPickedUpText != null &&
               WinsText != null &&
               LossesText != null &&
               DistDrivenText != null &&
               MatchesCountText != null &&
               WinPercentageText != null;
    }

    private GameObject FindStatsScreen()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && current.name == "PlayerStatsScreen")
                return current.gameObject;
        }

        return null;
    }

    private void BindTextIfMissing(ref TextMeshProUGUI target, params string[] pathsOrNames)
    {
        if (target != null || statsScreen == null)
            return;

        for (int i = 0; i < pathsOrNames.Length; i++)
        {
            Transform child = statsScreen.transform.Find(pathsOrNames[i]);
            if (child == null)
                child = FindChildByName(statsScreen.transform, pathsOrNames[i]);

            if (child == null)
                continue;

            target = child.GetComponent<TextMeshProUGUI>();
            if (target != null)
                return;
        }
    }

    private static Transform FindChildByName(Transform root, string name)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == name)
                return child;

            Transform nested = FindChildByName(child, name);
            if (nested != null)
                return nested;
        }

        return null;
    }

    void SaveStats()
    {
        SaveOppsElim();
        SaveTimesElim();
        SavePowerUpsPickedUp();
        SaveWins();
        SaveLosses();
        //SaveDistDriven();
    }

    public void IncOppsElim()
    {
        OppsElim++;
        SaveOppsElim();
    }

    public void IncTimesElim()
    {
        TimesElim++;
        SaveTimesElim();
    }

    public void IncPowerUpsPickedUp()
    {
        PowerUpsPickedUp++;
        SavePowerUpsPickedUp();
    }

    public void IncWins()
    {
        Wins++;
        SaveWins();
    }

    public void IncLosses()
    {
        Losses++;
        SaveLosses();
    }

    public void IncDistDriven(float totalDistance)
    {
        DistDriven = DistDriven + (totalDistance / 1000);
        SaveDistDriven();
    }

    void SaveOppsElim()
    {
        PlayerPrefs.SetInt("StatOppsElim", OppsElim);
        PlayerPrefs.Save();
        RefreshStatsUi();
    }

    void SaveTimesElim()
    {
        PlayerPrefs.SetInt("StatTimesElim", TimesElim);
        PlayerPrefs.Save();
        RefreshStatsUi();
    }

    void SavePowerUpsPickedUp()
    {
        PlayerPrefs.SetInt("StatPowerUpsPickedUp", PowerUpsPickedUp);
        PlayerPrefs.Save();
        RefreshStatsUi();
    }

    void SaveWins()
    {
        PlayerPrefs.SetInt("StatWins", Wins);
        PlayerPrefs.Save();
        RefreshStatsUi();
    }

    void SaveLosses()
    {
        PlayerPrefs.SetInt("StatLosses", Losses);
        PlayerPrefs.Save();
        RefreshStatsUi();
    }

    public void SaveDistDriven()
    {
        PlayerPrefs.SetFloat("StatDistDriven", DistDriven);
        PlayerPrefs.Save();
        RefreshStatsUi();
    }
}
