using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;

public class GameOverResultRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Image colorSwatch;
    [SerializeField] private GameObject highlightRoot;

    public void BindHeader()
    {
        if (rankText != null)
        {
            rankText.enabled = true;
            rankText.text = "#";
        }

        if (nameText != null)
        {
            nameText.enabled = true;
            nameText.text = "Player";
        }

        if (statsText != null)
        {
            statsText.enabled = true;
            statsText.text = "Kills\tDeaths\tK/D";
        }

        if (colorSwatch != null)
            colorSwatch.gameObject.SetActive(false);

        if (highlightRoot != null)
            highlightRoot.SetActive(false);
    }

    public void Bind(int rank, GameOverPayload.MatchResult result, Color accentColor)
    {
        if (result == null)
            return;

        if (rankText != null)
        {
            rankText.enabled = true;
            rankText.text = rank.ToString()+".";
            rankText.color = result.trailColor;
        }

        if (nameText != null)
        {
            nameText.enabled = true;
            nameText.text = string.IsNullOrWhiteSpace(result.displayName) ? "Unknown" : result.displayName;
            nameText.color = result.trailColor;
        }

        if (statsText != null)
        {
            statsText.enabled = true;
            float kdratio = result.kills / Mathf.Max(1, result.deaths);
            statsText.text = $"{result.kills}\t{result.deaths}\t\t{kdratio:0.00}";
            statsText.color = result.trailColor;
        }

        if (colorSwatch != null)
            colorSwatch.color = result.trailColor;

        if (highlightRoot != null)
            highlightRoot.SetActive(rank == 1);

        ApplyAccent(accentColor);
    }

    private void ApplyAccent(Color accentColor)
    {
        if (accentColor.a <= 0f)
            return;

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null || graphic == colorSwatch)
                continue;

            if (graphic is Image image)
            {
                Color color = image.color;
                color.r = Mathf.Clamp01(color.r * accentColor.r);
                color.g = Mathf.Clamp01(color.g * accentColor.g);
                color.b = Mathf.Clamp01(color.b * accentColor.b);
                image.color = color;
            }
        }
    }
}
