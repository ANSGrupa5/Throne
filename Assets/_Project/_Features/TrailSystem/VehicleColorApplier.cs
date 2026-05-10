using UnityEngine;

public class VehicleColorApplier : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Color color = Color.white;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private MaterialPropertyBlock _propertyBlock;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(true);
    }

    public void SetColor(Color newColor)
    {
        color = newColor;
        Apply();
    }

    public Color GetColor()
    {
        return color;
    }

    private void OnEnable()
    {
        EnsureInitialized();
        Apply();
    }

    private void Apply()
    {
        if (_propertyBlock == null)
            _propertyBlock = new MaterialPropertyBlock();

        if (renderers == null)
            return;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, color);
            _propertyBlock.SetColor(ColorId, color);
            renderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
