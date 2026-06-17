using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class RuntimePopupDialog : MonoBehaviour
{
    private static RuntimePopupDialog _activeDialog;

    private Action _onOk;

    public static void Show(string message, Action onOk = null)
    {
        Show(message, "OK", onOk);
    }

    public static void Show(string message, string buttonText, Action onOk = null)
    {
        if (_activeDialog != null)
            Destroy(_activeDialog.gameObject);

        GameObject root = new GameObject("RuntimePopupDialog");
        RuntimePopupDialog dialog = root.AddComponent<RuntimePopupDialog>();
        dialog._onOk = onOk;
        dialog.Build(message, buttonText);
        _activeDialog = dialog;
    }

    private void Build(string message, string okButtonText)
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 600f);
        scaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        GameObject dimmer = CreateUiObject("Dimmer", transform);
        Image dimmerImage = dimmer.AddComponent<Image>();
        dimmerImage.color = new Color(0f, 0f, 0f, 0.55f);
        Stretch(dimmer.GetComponent<RectTransform>());

        GameObject panel = CreateUiObject("Panel", dimmer.transform);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.09f, 0.11f, 0.96f);

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(480f, 250f);

        GameObject messageObject = CreateUiObject("Message", panel.transform);
        Text messageText = messageObject.AddComponent<Text>();
        messageText.font = Font.CreateDynamicFontFromOSFont("Arial", 96);
        messageText.fontSize = 38;
        messageText.text = message;
        messageText.color = Color.white;
        messageText.alignment = TextAnchor.MiddleCenter;
        messageText.horizontalOverflow = HorizontalWrapMode.Wrap;
        messageText.verticalOverflow = VerticalWrapMode.Truncate;

        RectTransform messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.08f, 0.34f);
        messageRect.anchorMax = new Vector2(0.92f, 0.9f);
        messageRect.offsetMin = Vector2.zero;
        messageRect.offsetMax = Vector2.zero;

        GameObject buttonObject = CreateUiObject("OkButton", panel.transform);
        Image buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.color = new Color(0.17f, 0.45f, 0.9f, 1f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(CloseWithOk);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.14f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.14f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonRect.sizeDelta = new Vector2(180f, 56f);

        GameObject buttonTextObject = CreateUiObject("Text", buttonObject.transform);
        Text buttonText = buttonTextObject.AddComponent<Text>();
        buttonText.font = Font.CreateDynamicFontFromOSFont("Arial", 72);
        buttonText.fontSize = 32;
        buttonText.text = string.IsNullOrWhiteSpace(okButtonText) ? "OK" : okButtonText;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        Stretch(buttonTextObject.GetComponent<RectTransform>());
    }

    private void CloseWithOk()
    {
        Action callback = _onOk;
        _onOk = null;

        if (_activeDialog == this)
            _activeDialog = null;

        Destroy(gameObject);
        callback?.Invoke();
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
