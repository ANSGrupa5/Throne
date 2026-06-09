using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuButtonPrefabConverter
{
    private const string ScenePath = "Assets/Project/Scenes/UI/Menu/MainMenu.unity";
    private const string ButtonPrefabPath = "Assets/Project/Prefabs/UI/Menu/Button.prefab";
    private const string BackButtonPrefabPath = "Assets/Project/Prefabs/UI/Menu/BackButton.prefab";
    private const string SettingsIconPath = "Assets/Project/Art/UI/Icons/Icon Material/settings.png";
    private const string ExitIconPath = "Assets/Project/Art/UI/Icons/Icon Material/exit.png";
    private const string IconChildName = "ButtonIcon";

    private static readonly Color NormalColor = ColorFromHtml("#00FEEE");
    private static readonly Color HoverColor = ColorFromHtml("#1EFFE6");
    private static readonly Color PressedColor = ColorFromHtml("#00B8C7");
    private static readonly Color DisabledColor = new(0f, 254f / 255f, 238f / 255f, 0.5f);
    private static readonly Color BackNormalColor = ColorFromHtml("#A7B1B7");
    private static readonly Color BackHoverColor = ColorFromHtml("#D2DCDF");
    private static readonly Color BackPressedColor = ColorFromHtml("#6F7E84");
    private static readonly Color BackDisabledColor = new(167f / 255f, 177f / 255f, 183f / 255f, 0.5f);
    private static readonly Color BackTextColor = ColorFromHtml("#E2E7E9");
    private static readonly Color OptionsIconColor = ColorFromHtml("#FBF900");
    private static readonly Color ExitIconColor = ColorFromHtml("#FF003D");

    [MenuItem("Throne/Tools/Convert Main Menu Buttons To Prefabs")]
    public static void Convert()
    {
        UpdateButtonPrefab();
        UpdateBackButtonPrefab();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (buttonPrefab == null)
            throw new System.InvalidOperationException($"Missing prefab at {ButtonPrefabPath}");

        GameObject backButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackButtonPrefabPath);
        if (backButtonPrefab == null)
            throw new System.InvalidOperationException($"Missing prefab at {BackButtonPrefabPath}");

        ConvertEligibleButtons(buttonPrefab, backButtonPrefab);
        RefreshSpecialButtonOverrides();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Throne/Tools/Refresh Main Menu Button Styles")]
    public static void RefreshStyles()
    {
        UpdateButtonPrefab();
        UpdateBackButtonPrefab();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RefreshSpecialButtonOverrides();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
    }

    private static void UpdateButtonPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ButtonPrefabPath);
        try
        {
            ConfigureButtonVisuals(root);
            ConfigureButtonFeedback(root);
            EnsureOptionalIcon(root);
            PrefabUtility.SaveAsPrefabAsset(root, ButtonPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void UpdateBackButtonPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(BackButtonPrefabPath);
        try
        {
            ConfigureBackButtonVisuals(root);
            ConfigureButtonFeedback(root);
            PrefabUtility.SaveAsPrefabAsset(root, BackButtonPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ConvertEligibleButtons(GameObject buttonPrefab, GameObject backButtonPrefab)
    {
        Transform menuRoot = FindTransformByName("MainMenu");
        if (menuRoot == null)
            throw new System.InvalidOperationException("Could not find MainMenu in MainMenu scene.");

        Button[] sceneButtons = menuRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < sceneButtons.Length; i++)
        {
            Button sourceButton = sceneButtons[i];
            if (!ShouldConvert(sourceButton))
                continue;

            GameObject prefab = sourceButton.gameObject.name == "BackButton" ? backButtonPrefab : buttonPrefab;
            ReplaceButton(sourceButton, prefab);
        }
    }

    private static bool ShouldConvert(Button sourceButton)
    {
        if (sourceButton == null)
            return false;

        GameObject go = sourceButton.gameObject;
        string objectName = go.name;
        if (go.GetComponent<VolumeButton>() != null)
            return false;

        if (go.GetComponentInParent<TMP_Dropdown>(true) != null ||
            go.GetComponentInParent<Slider>(true) != null ||
            go.GetComponentInParent<Toggle>(true) != null ||
            go.GetComponentInParent<Scrollbar>(true) != null)
            return false;

        return objectName == "BackButton" ||
               objectName == "MenuButton" ||
               objectName.EndsWith("Button", System.StringComparison.Ordinal);
    }

    private static void ReplaceButton(Button sourceButton, GameObject prefab)
    {
        GameObject existing = sourceButton.gameObject;
        Transform parent = existing.transform.parent;
        RectTransform existingRect = existing.transform as RectTransform;
        int siblingIndex = existing.transform.GetSiblingIndex();
        string buttonName = existing.name;
        string label = GetLabelText(existing);
        SerializedObject sourceButtonData = new(sourceButton);
        SerializedProperty sourceOnClick = sourceButtonData.FindProperty("m_OnClick");

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = buttonName;
        instance.transform.SetSiblingIndex(siblingIndex);

        RectTransform instanceRect = instance.GetComponent<RectTransform>();
        CopyRectTransform(existingRect, instanceRect);

        TMP_Text text = instance.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;

        Button targetButton = instance.GetComponent<Button>();
        if (targetButton == null)
            throw new System.InvalidOperationException($"{prefab.name} does not contain a Button component.");

        targetButton.interactable = sourceButton.interactable;
        targetButton.navigation = sourceButton.navigation;
        CopyPersistentClick(sourceOnClick, targetButton);
        ApplySpecialIconOverride(instance, buttonName);

        Object.DestroyImmediate(existing);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
    }

    private static void ConfigureButtonVisuals(GameObject root)
    {
        Image image = root.GetComponent<Image>();
        if (image != null)
        {
            image.color = NormalColor;
            image.raycastTarget = true;
        }

        Button button = root.GetComponent<Button>();
        if (button != null)
            button.colors = CreateButtonColors();

        TMP_Text label = root.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.color = NormalColor;
    }

    private static void ConfigureBackButtonVisuals(GameObject root)
    {
        Image image = root.GetComponent<Image>();
        if (image != null)
        {
            image.color = BackNormalColor;
            image.material = null;
            image.raycastTarget = true;
        }

        Button button = root.GetComponent<Button>();
        if (button != null)
            button.colors = CreateBackButtonColors();

        TMP_Text label = root.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = BackTextColor;
            label.fontStyle = FontStyles.Bold;
            label.fontWeight = FontWeight.Bold;
            label.raycastTarget = false;
        }

        Transform iconTransform = root.transform.Find("Back Icon");
        if (iconTransform != null && iconTransform.TryGetComponent(out Image icon))
        {
            icon.color = BackTextColor;
            icon.raycastTarget = false;
            icon.preserveAspect = true;
        }
    }

    private static void ConfigureButtonFeedback(GameObject root)
    {
        ButtonFeedback feedback = root.GetComponent<ButtonFeedback>();
        if (feedback == null)
            return;

        feedback.HoverVolume = 0.55f;
        feedback.ClickVolume = 1f;
    }

    private static void EnsureOptionalIcon(GameObject root)
    {
        Transform existingIcon = root.transform.Find(IconChildName);
        if (existingIcon == null)
        {
            GameObject iconObject = new(IconChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            existingIcon = iconObject.transform;
            existingIcon.SetParent(root.transform, false);
        }

        RectTransform iconRect = existingIcon as RectTransform;
        if (iconRect != null)
        {
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(57.2f, 0f);
            iconRect.sizeDelta = new Vector2(40f, 40f);
            iconRect.localScale = Vector3.one * 0.43f;
        }

        Image icon = existingIcon.GetComponent<Image>();
        if (icon != null)
        {
            icon.raycastTarget = false;
            icon.color = Color.white;
            icon.sprite = null;
            icon.preserveAspect = true;
        }

        existingIcon.gameObject.SetActive(false);
    }

    private static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        if (source == null || target == null)
            return;

        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
        target.localPosition = source.localPosition;
    }

    private static string GetLabelText(GameObject root)
    {
        TMP_Text label = root.GetComponentInChildren<TMP_Text>(true);
        return label != null ? label.text : string.Empty;
    }

    private static void CopyPersistentClick(SerializedProperty sourceOnClick, Button targetButton)
    {
        SerializedObject targetButtonData = new(targetButton);
        targetButtonData.CopyFromSerializedProperty(sourceOnClick);
        targetButtonData.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ApplySpecialIconOverride(GameObject instance, string buttonName)
    {
        if (buttonName != "OptionsButton" && buttonName != "ExitButton")
        {
            SetIconActive(instance, false);
            return;
        }

        string spritePath = buttonName == "OptionsButton" ? SettingsIconPath : ExitIconPath;
        Color iconColor = buttonName == "OptionsButton" ? OptionsIconColor : ExitIconColor;
        Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (iconSprite == null)
            throw new System.InvalidOperationException($"Missing icon sprite at {spritePath}");

        Image icon = GetOptionalIcon(instance);
        icon.sprite = iconSprite;
        icon.color = iconColor;
        icon.gameObject.SetActive(true);
        PrefabUtility.RecordPrefabInstancePropertyModifications(icon);

        TMP_Text label = instance.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.color = iconColor;
            PrefabUtility.RecordPrefabInstancePropertyModifications(label);
        }
    }

    private static void RefreshSpecialButtonOverrides()
    {
        Transform menuRoot = FindTransformByName("MainMenu");
        if (menuRoot == null)
            throw new System.InvalidOperationException("Could not find MainMenu in MainMenu scene.");

        Button[] sceneButtons = menuRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < sceneButtons.Length; i++)
        {
            Button button = sceneButtons[i];
            if (button == null)
                continue;

            string buttonName = button.gameObject.name;
            if (buttonName == "OptionsButton" || buttonName == "ExitButton")
                ApplySpecialIconOverride(button.gameObject, buttonName);
        }
    }

    private static void SetIconActive(GameObject instance, bool active)
    {
        Transform icon = instance.transform.Find(IconChildName);
        if (icon != null)
            icon.gameObject.SetActive(active);
    }

    private static Image GetOptionalIcon(GameObject instance)
    {
        Transform iconTransform = instance.transform.Find(IconChildName);
        if (iconTransform == null)
            throw new System.InvalidOperationException($"{instance.name} is missing {IconChildName}.");

        Image icon = iconTransform.GetComponent<Image>();
        if (icon == null)
            throw new System.InvalidOperationException($"{IconChildName} is missing an Image component.");

        return icon;
    }

    private static Transform FindTransformByName(string name)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform transform = transforms[i];
            if (transform != null && transform.name == name)
                return transform;
        }

        return null;
    }

    private static ColorBlock CreateButtonColors()
    {
        return new ColorBlock
        {
            normalColor = NormalColor,
            highlightedColor = HoverColor,
            pressedColor = PressedColor,
            selectedColor = HoverColor,
            disabledColor = DisabledColor,
            colorMultiplier = 1f,
            fadeDuration = 0.22f
        };
    }

    private static ColorBlock CreateBackButtonColors()
    {
        return new ColorBlock
        {
            normalColor = BackNormalColor,
            highlightedColor = BackHoverColor,
            pressedColor = BackPressedColor,
            selectedColor = BackHoverColor,
            disabledColor = BackDisabledColor,
            colorMultiplier = 1f,
            fadeDuration = 0.18f
        };
    }

    private static Color ColorFromHtml(string html)
    {
        if (!ColorUtility.TryParseHtmlString(html, out Color color))
            throw new System.ArgumentException($"Invalid color value: {html}", nameof(html));

        return color;
    }
}
