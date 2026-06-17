using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class GameOverResultRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Image colorSwatch;
    [SerializeField] private GameObject highlightRoot;

    [Header("Colors")]
    [SerializeField] private Color headerTextColor = Color.white;
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color winnerTextColor = new Color(1f, 0.86f, 0.25f, 1f);
    [SerializeField] private Color fallbackSwatchColor = Color.white;

    public void BindHeader()
    {
        if (rankText != null)
        {
            rankText.enabled = true;
            rankText.text = "#";
            rankText.color = headerTextColor;
        }

        if (nameText != null)
        {
            nameText.enabled = true;
            nameText.text = "Player";
            nameText.color = headerTextColor;
        }

        if (statsText != null)
        {
            statsText.enabled = true;
            statsText.text = "Kills\tDeaths\tK/D";
            statsText.color = headerTextColor;
        }

        if (colorSwatch != null)
            colorSwatch.gameObject.SetActive(false);

        if (highlightRoot != null)
            highlightRoot.SetActive(false);
    }

    public void Bind(int rank, GameOverPayload.MatchResult result, bool isWinner)
    {
        if (result == null)
            return;

        Color textColor = SanitizeDisplayColor(result.trailColor, isWinner ? winnerTextColor : normalTextColor);

        if (rankText != null)
        {
            rankText.enabled = true;
            rankText.text = rank.ToString()+".";
            rankText.color = textColor;
        }

        if (nameText != null)
        {
            nameText.enabled = true;
            nameText.text = string.IsNullOrWhiteSpace(result.displayName) ? "Unknown" : result.displayName;
            nameText.color = textColor;
        }

        if (statsText != null)
        {
            statsText.enabled = true;
            float kdratio = (float)Mathf.Max(1, result.kills) / (float)Mathf.Max(1, result.deaths);
            statsText.text = $"{result.kills}\t{result.deaths}\t\t{kdratio:0.00}";
            statsText.color = textColor;
        }

        if (colorSwatch != null)
        {
            colorSwatch.gameObject.SetActive(true);
            colorSwatch.color = SanitizeDisplayColor(result.trailColor, fallbackSwatchColor);
        }

        if (highlightRoot != null)
            highlightRoot.SetActive(isWinner);

        ApplyAccent(isWinner ? winnerTextColor : Color.white);
    }

    private static Color SanitizeDisplayColor(Color color, Color fallback)
    {
        if (color.a <= 0.01f)
            color = fallback;

        color.a = 1f;
        return color;
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
