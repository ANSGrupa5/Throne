using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MatchResultRow : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private Image colorSwatch;
    [SerializeField] private GameObject highlightRoot;

    [Header("Rows")]
    [SerializeField] private Image rowBackground;
    [SerializeField] private Color headerBackground = new(0f, 0.9f, 1f, 0.16f);
    [SerializeField] private Color evenBackground = new(0.015f, 0.055f, 0.07f, 0.72f);
    [SerializeField] private Color oddBackground = new(0.025f, 0.075f, 0.095f, 0.82f);
    [SerializeField] private Color winnerBackground = new(1f, 0.84f, 0.16f, 0.18f);

    private void Awake()
    {
        ResolveBackground();
    }

    public void BindHeader()
    {
        ResolveBackground();
        ApplyBackground(headerBackground);

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

    public void Bind(int rank, GameOverPayload.MatchResult result, Color accentColor, bool alternate)
    {
        if (result == null)
            return;

        ResolveBackground();
        ApplyBackground(rank == 1 ? winnerBackground : alternate ? oddBackground : evenBackground);

        if (rankText != null)
        {
            rankText.enabled = true;
            rankText.text = rank.ToString() + ".";
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
            float kdratio = (float)result.kills / Mathf.Max(1, result.deaths);
            statsText.text = $"{result.kills}\t{result.deaths}\t\t{kdratio:0.00}";
            statsText.color = result.trailColor;
        }

        if (colorSwatch != null)
            colorSwatch.color = result.trailColor;

        if (highlightRoot != null)
            highlightRoot.SetActive(rank == 1);

        ApplyAccent(accentColor);
    }

    private void ResolveBackground()
    {
        if (rowBackground != null)
            return;

        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image != null && image != colorSwatch)
            {
                rowBackground = image;
                return;
            }
        }
    }

    private void ApplyBackground(Color color)
    {
        if (rowBackground != null)
            rowBackground.color = color;
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
