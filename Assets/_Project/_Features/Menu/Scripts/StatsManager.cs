using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;

    [SerializeField] private TextMeshProUGUI OppsElimText;
    [SerializeField] private TextMeshProUGUI TimesElimText;
    [SerializeField] private TextMeshProUGUI PowerUpsPickedUpText;
    [SerializeField] private TextMeshProUGUI WinsText;
    [SerializeField] private TextMeshProUGUI LossesText;
    [SerializeField] private TextMeshProUGUI DistDrivenText;

    private GameObject StatisticsScreen;
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

        LoadStats();
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
        player = GameObject.Find("motorFINAL2_WORKING(Clone)");
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
        if (player == objectToCheck.gameObject)
            return true;
        return false;
    }

    public bool CheckIfPlayerPickedUpPowerUp(VehicleLife objectToCheck)
    {
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
        StatisticsScreen = GameObject.Find("MainMenuObject/Canvas/Panel/MainMenu/StatisticsScreen").gameObject;
        OppsElimText = StatisticsScreen.transform.Find("StatsRowLeft/Stat_Opps_Elim/Value").GetComponent<TextMeshProUGUI>();
        TimesElimText = StatisticsScreen.transform.Find("StatsRowLeft/Stat_Times_Elim/Value").GetComponent<TextMeshProUGUI>();
        PowerUpsPickedUpText = StatisticsScreen.transform.Find("StatsRowLeft/Stat_Total_Pow/Value").GetComponent<TextMeshProUGUI>();
        WinsText = StatisticsScreen.transform.Find("StatsRowRight/Stat_Wins/Value").GetComponent<TextMeshProUGUI>();
        LossesText = StatisticsScreen.transform.Find("StatsRowRight/Stat_Losses/Value").GetComponent<TextMeshProUGUI>();
        DistDrivenText = StatisticsScreen.transform.Find("StatsRowRight/Stat_Dist_Driven/Value").GetComponent<TextMeshProUGUI>();

        OppsElim = PlayerPrefs.GetInt("StatOppsElim", 0);
        OppsElimText.text = OppsElim.ToString();

        TimesElim = PlayerPrefs.GetInt("StatTimesElim", 0);
        TimesElimText.text = TimesElim.ToString();

        PowerUpsPickedUp = PlayerPrefs.GetInt("StatPowerUpsPickedUp", 0);
        PowerUpsPickedUpText.text = PowerUpsPickedUp.ToString();

        Wins = PlayerPrefs.GetInt("StatWins", 0);
        WinsText.text = Wins.ToString();

        Losses = PlayerPrefs.GetInt("StatLosses", 0);
        LossesText.text = Losses.ToString();

        DistDriven = PlayerPrefs.GetFloat("StatDistDriven", 0f);
        DistDrivenText.text = $"{DistDriven:F2} km";
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
    }

    void SaveTimesElim()
    {
        PlayerPrefs.SetInt("StatTimesElim", TimesElim);
        PlayerPrefs.Save();
    }

    void SavePowerUpsPickedUp()
    {
        PlayerPrefs.SetInt("StatPowerUpsPickedUp", PowerUpsPickedUp);
        PlayerPrefs.Save();
    }

    void SaveWins()
    {
        PlayerPrefs.SetInt("StatWins", Wins);
        PlayerPrefs.Save();
    }

    void SaveLosses()
    {
        PlayerPrefs.SetInt("StatLosses", Losses);
        PlayerPrefs.Save();
    }

    public void SaveDistDriven()
    {
        PlayerPrefs.SetFloat("StatDistDriven", DistDriven);
        PlayerPrefs.Save();
    }
}
