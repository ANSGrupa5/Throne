using TMPro;
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

    void LoadStats()
    {
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

        DistDriven = PlayerPrefs.GetFloat("StatOppsElim", 0f);
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

    void IncOppsElim()
    {
        OppsElim++;
    }

    void IncTimesElim()
    {
        TimesElim++;
    }

    void IncPowerUpsPickedUp()
    {
        PowerUpsPickedUp++;
    }

    void IncWins()
    {
        Wins++;
    }

    void IncLosses()
    {
        Losses++;
    }

    /*
    void IncDistDriven()
    {

    }
    */

    void SaveOppsElim()
    {
        PlayerPrefs.SetInt("StatOppsElim", OppsElim);
    }

    void SaveTimesElim()
    {
        PlayerPrefs.SetInt("StatTimesElim", TimesElim);
    }

    void SavePowerUpsPickedUp()
    {
        PlayerPrefs.SetInt("StatPowerUpsPickedUp", PowerUpsPickedUp);
    }

    void SaveWins()
    {
        PlayerPrefs.SetInt("StatWins", Wins);
    }

    void SaveLosses()
    {
        PlayerPrefs.SetInt("StatLosses", Losses);
    }

    /*
    void SaveDistDriven()
    {

    }
    */
}
