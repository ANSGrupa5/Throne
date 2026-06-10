using UnityEngine;

public class AreaBoundaryScript : MonoBehaviour
{
    public enum ArenaStyle
    {
        Circle,
        Square
    }

    public ArenaStyle style;

    public float defaultSize = 10f;
    public float minSize = 3f;
    public float shrinkSpeed = 2f;

    public bool isShrinking = false;

    [Header("Circle (FBX)")]
    public GameObject RingObject;
    public Transform visualModel; // ring (FBX)

    [Header("Square (Walls)")]
    public GameObject SquareObject;
    public Transform wallTop;
    public Transform wallBottom;
    public Transform wallLeft;
    public Transform wallRight;

    public float wallThickness = 0.5f;
    public float wallHeight = 3f;

    [Header("Visual Effects")]
    public Renderer targetRenderer;

    public float minAlpha = 0.5f;
    public float maxAlpha = 1f;
    public float blinkSpeed = 2f;

    Renderer[] renderers;

    private float currentSize;
    private Material mat;

    private void Start()
    {
        currentSize = defaultSize;

        if (style == ArenaStyle.Circle)
        {
            SquareObject.SetActive(false);

            if (visualModel == null && transform.childCount > 0)
            {
                visualModel = transform.GetChild(0);
            }

            // ?? DOPIERO TERAZ pobieramy renderery
            renderers = visualModel.GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
                mat = renderers[0].material;

            SetCircleSize(currentSize);
        }
        else
        {
            RingObject.SetActive(false);

            SetupSquareWalls();
            UpdateWalls();

            // je�li chcesz efekt te� na �cianach:
            renderers = SquareObject.GetComponentsInChildren<Renderer>();

            if (renderers.Length > 0)
                mat = renderers[0].material;
        }
    }

    private void Update()
    {
        if (!isShrinking) return;
        if (!RingObject.activeInHierarchy || !SquareObject.activeInHierarchy)
        {
            RingObject.SetActive(style == ArenaStyle.Circle);
            SquareObject.SetActive(style == ArenaStyle.Square);
        }
        if (currentSize > minSize)
        {
            currentSize -= shrinkSpeed * Time.deltaTime;
            currentSize = Mathf.Max(currentSize, minSize);

            if (style == ArenaStyle.Circle)
            {
                SetCircleSize(currentSize);
            }
            else
            {
                UpdateWalls();
            }
            UpdateVisualEffect();
        }
    }

    void SetAlpha(float alpha)
    {
        foreach (var r in renderers)
        {
            var mat = r.material;

            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
            else if (mat.HasProperty("_Color"))
            {
                Color c = mat.GetColor("_Color");
                c.a = alpha;
                mat.SetColor("_Color", c);
            }
        }
    }

    void UpdateVisualEffect()
    {
        if (renderers == null || renderers.Length == 0) return;

        float t = Mathf.Sin(Time.time * blinkSpeed) * 0.5f + 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        SetAlpha(alpha);
    }

    // =========================
    // CIRCLE (FBX)
    // =========================
    void SetCircleSize(float size)
    {
        // skalujemy tylko X/Z (arena pozioma)
        transform.localScale = new Vector3(size, 1f, size);
    }

    // =========================
    // SQUARE (4 �CIANY)
    // =========================
    void SetupSquareWalls()
    {
        // upewnij si� �e �ciany maj� collidery
        SetupWall(wallTop);
        SetupWall(wallBottom);
        SetupWall(wallLeft);
        SetupWall(wallRight);
    }

    void SetupWall(Transform wall)
    {
        if (wall == null) return;

        if (wall.GetComponent<BoxCollider>() == null)
        {
            wall.gameObject.AddComponent<BoxCollider>();
        }
    }

    void UpdateWalls()
    {
        float half = currentSize / 2f;

        // TOP
        wallTop.localPosition = new Vector3(0, 0, half);
        wallTop.localScale = new Vector3(currentSize, wallHeight, wallThickness);

        // BOTTOM
        wallBottom.localPosition = new Vector3(0, 0, -half);
        wallBottom.localScale = new Vector3(currentSize, wallHeight, wallThickness);

        // LEFT
        wallLeft.localPosition = new Vector3(-half, 0, 0);
        wallLeft.localScale = new Vector3(wallThickness, wallHeight, currentSize);

        // RIGHT
        wallRight.localPosition = new Vector3(half, 0, 0);
        wallRight.localScale = new Vector3(wallThickness, wallHeight, currentSize);
    }
}
