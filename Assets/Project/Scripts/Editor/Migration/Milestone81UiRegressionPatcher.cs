using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class Milestone81UiRegressionPatcher
{
    private const string MainMenuScenePath = "Assets/Project/Scenes/UI/Menu/MainMenu.unity";
    private const string MultiplayerConnectionScenePath = "Assets/Project/Scenes/UI/Menu/MultiplayerConnection.unity";
    private const string LobbyScenePath = "Assets/Project/Scenes/UI/Lobby/LobbyScene.unity";
    private const string GameOverScenePath = "Assets/Project/Scenes/UI/Menu/GameOver.unity";

    private const string SingleplayerLobbyRootPrefabPath = "Assets/Project/Prefabs/UI/Lobby/SingleplayerLobbyRoot.prefab";
    private const string MultiplayerLobbyRootPrefabPath = "Assets/Project/Prefabs/UI/Lobby/MultiplayerLobbyRoot.prefab";
    private const string LegacyLobbyRootPrefabPath = "Assets/Project/Prefabs/UI/Lobby/LobbyRoot.prefab";

    private const string DefaultPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Default.asset";
    private const string DarkerPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Darker.asset";
    private const string BackPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Back.asset";
    private const string AddBotPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_AddBot.asset";

    [MenuItem("Throne/Tools/Migrations/DO NOT RUN - Legacy Migration/Run Milestone 8.1 UI Regression Fixes")]
    public static void Run()
    {
        if (!ConfirmLegacyRun("Milestone 8.1 UI Regression Fixes"))
            return;

        PatchPresets();
        PatchUiPrefabs();
        PatchLegacyLobbyRootPrefab();
        PatchLobbyRootPrefab(SingleplayerLobbyRootPrefabPath, false);
        PatchLobbyRootPrefab(MultiplayerLobbyRootPrefabPath, true);
        PatchMainMenuScene();
        PatchMultiplayerConnectionScene();
        PatchSceneSelectables(GameOverScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Milestone81UiRegressionPatcher] Patch complete.");
    }

    private static bool ConfirmLegacyRun(string migrationName)
    {
        const string message = "This is a legacy one-shot global UI migration. It can reapply broad colors, layout changes, and target-graphic policies. Use the Milestone 8.2 targeted repair/verifier instead unless you are intentionally replaying history.";
        if (Application.isBatchMode)
        {
            Debug.LogError($"[{nameof(Milestone81UiRegressionPatcher)}] Blocked legacy migration '{migrationName}' in batch mode. {message}");
            return false;
        }

        return EditorUtility.DisplayDialog(
            $"Legacy migration: {migrationName}",
            message,
            "Run Legacy Migration",
            "Cancel");
    }

    [MenuItem("Throne/Tools/Migrations/Verify Milestone 8.1 UI Regression Fixes")]
    public static void Verify()
    {
        VerifyNoMissingScripts();
        VerifyMainMenuActions();
        VerifySelectionPolicies();
        VerifyLobbyAtmosphere();
        VerifyMultiplayerConnection();
        VerifyStatsScreen();
        Debug.Log("[Milestone81UiRegressionPatcher] Verification complete.");
    }

    public static void RunAndVerify()
    {
        Run();
        Verify();
    }

    private static void PatchPresets()
    {
        ConfigurePreset(DefaultPresetPath,
            new Color(0.055f, 0.28f, 0.32f, 0.96f),
            new Color(0.18f, 0.88f, 0.96f, 1f),
            new Color(0.05f, 0.48f, 0.58f, 1f),
            new Color(0.12f, 0.68f, 0.78f, 1f),
            new Color(0.035f, 0.055f, 0.06f, 0.44f));

        ConfigurePreset(DarkerPresetPath,
            new Color(0.035f, 0.08f, 0.095f, 0.94f),
            new Color(0.1f, 0.28f, 0.32f, 1f),
            new Color(0.05f, 0.17f, 0.2f, 1f),
            new Color(0.08f, 0.23f, 0.27f, 1f),
            new Color(0.02f, 0.03f, 0.035f, 0.46f));

        ConfigurePreset(BackPresetPath,
            new Color(0.055f, 0.07f, 0.085f, 0.92f),
            new Color(0.16f, 0.19f, 0.22f, 1f),
            new Color(0.08f, 0.1f, 0.12f, 1f),
            new Color(0.12f, 0.15f, 0.18f, 1f),
            new Color(0.025f, 0.03f, 0.035f, 0.5f));

        ConfigurePreset(AddBotPresetPath,
            new Color(0.045f, 0.2f, 0.17f, 0.92f),
            new Color(0.18f, 0.72f, 0.58f, 1f),
            new Color(0.07f, 0.38f, 0.32f, 1f),
            new Color(0.11f, 0.52f, 0.44f, 1f),
            new Color(0.025f, 0.05f, 0.045f, 0.48f));
    }

    private static void ConfigurePreset(string path, Color normal, Color highlighted, Color pressed, Color selected, Color disabled)
    {
        MenuSelectableVisualPreset preset = AssetDatabase.LoadAssetAtPath<MenuSelectableVisualPreset>(path);
        if (preset == null)
        {
            EnsureFolder(Path.GetDirectoryName(path)?.Replace('\\', '/'));
            preset = ScriptableObject.CreateInstance<MenuSelectableVisualPreset>();
            AssetDatabase.CreateAsset(preset, path);
        }

        preset.NormalColor = normal;
        preset.HighlightedColor = highlighted;
        preset.PressedColor = pressed;
        preset.SelectedColor = selected;
        preset.DisabledColor = disabled;
        preset.ColorMultiplier = 1f;
        preset.FadeDuration = 0.08f;
        EditorUtility.SetDirty(preset);
    }

    private static void PatchUiPrefabs()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project/Prefabs/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                PatchSelectablePolicies(root, path);
                if (path.Contains("/Menu/Screens/") || path.Contains("/Menu/Rows/"))
                    PatchMainMenuPrefab(root);
                if (path.Contains("MatchSettingsPanel"))
                    PatchMatchSettingsLabels(root);
                if (path.Contains("TrailColorSelectionPanel"))
                    PatchTrailColorLayout(root);

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void PatchLobbyRootPrefab(string prefabPath, bool multiplayer)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            PatchSelectablePolicies(root, prefabPath);
            PatchLobbyAtmosphere(root, multiplayer);
            PatchLobbyPanelBrightness(root);
            PatchMatchSettingsLabels(root);
            PatchTrailColorLayout(root);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void PatchLegacyLobbyRootPrefab()
    {
        if (!File.Exists(LegacyLobbyRootPrefabPath))
            return;

        GameObject root = PrefabUtility.LoadPrefabContents(LegacyLobbyRootPrefabPath);
        try
        {
            PatchSelectablePolicies(root, LegacyLobbyRootPrefabPath);
            PatchMatchSettingsLabels(root);
            PatchTrailColorLayout(root);
            PrefabUtility.SaveAsPrefabAsset(root, LegacyLobbyRootPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void PatchMainMenuScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (controller == null)
            throw new InvalidOperationException("MainMenu scene is missing MainMenuController.");

        PatchSceneSelectablePolicies(MainMenuScenePath);
        PatchMainMenuButton(controller, "settings", controller.ShowSettings, "settings", true);
        PatchMainMenuButton(controller, "player stats", controller.ShowStatisticsScreen, "player stats", false);
        PatchMainMenuButton(controller, "exit", controller.Exit, "exit", true);
        PatchMainMenuBackdrop();
        PatchStatsScreen();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchMultiplayerConnectionScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MultiplayerConnectionScenePath, OpenSceneMode.Single);
        PatchSceneSelectablePolicies(MultiplayerConnectionScenePath);
        PatchMultiplayerConnectionBackground();
        PatchMultiplayerConnectionTitle();
        PatchMultiplayerConnectionCards();
        PatchMultiplayerConnectionBackButton();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchSceneSelectables(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        PatchSceneSelectablePolicies(scenePath);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchSceneSelectablePolicies(string contextPath)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            PatchSelectablePolicies(root, contextPath);
    }

    private static void PatchSelectablePolicies(GameObject root, string contextPath)
    {
        Selectable[] selectables = root.GetComponentsInChildren<Selectable>(true);
        foreach (Selectable selectable in selectables)
        {
            if (selectable == null)
                continue;

            if (IsContainerSelectable(selectable))
            {
                StripSelectableFeedback(selectable);
                continue;
            }

            MenuSelectionPersistence persistence = ResolvePersistence(selectable, contextPath);
            MenuSelectable menuSelectable = selectable.GetComponent<MenuSelectable>();
            if (menuSelectable == null)
                menuSelectable = selectable.gameObject.AddComponent<MenuSelectable>();

            SerializedObject menuSerialized = new(menuSelectable);
            SetObject(menuSerialized, "selectable", selectable);
            SetObject(menuSerialized, "targetGraphicOverride", ResolveTargetGraphic(selectable));
            SetObject(menuSerialized, "visualPreset", ResolveVisualPreset(selectable, contextPath));
            SetEnum(menuSerialized, "selectionPersistence", (int)persistence);
            SetBool(menuSerialized, "clearEventSystemSelectionOnPointerUp", persistence == MenuSelectionPersistence.None);
            SetBool(menuSerialized, "useColorTint", ResolveTargetGraphic(selectable) != null);
            SetBool(menuSerialized, "playClickSound", selectable is not TMP_Dropdown);
            menuSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menuSelectable);

            MenuSelectableStateGraphics stateGraphics = selectable.GetComponent<MenuSelectableStateGraphics>();
            if (stateGraphics == null)
                stateGraphics = selectable.gameObject.AddComponent<MenuSelectableStateGraphics>();

            SerializedObject stateSerialized = new(stateGraphics);
            SetObject(stateSerialized, "selectable", selectable);
            SetEnum(stateSerialized, "selectionPersistence", (int)persistence);
            if (!IsFullButton(selectable, contextPath))
                SetObject(stateSerialized, "hoverGlow", null);
            if (persistence != MenuSelectionPersistence.Persistent)
                SetObject(stateSerialized, "selectedGlow", null);
            stateSerialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(stateGraphics);

            selectable.targetGraphic = ResolveTargetGraphic(selectable);
            if (selectable.targetGraphic == null)
                selectable.transition = Selectable.Transition.None;
            EditorUtility.SetDirty(selectable);
        }
    }

    private static bool IsContainerSelectable(Selectable selectable)
    {
        string name = selectable.name;
        if (selectable is TMP_Dropdown or TMP_InputField or InputField or Slider or Toggle)
            return false;
        if (name.Contains("TrailColorButton") || name.Contains("BackButton") || name.Contains("Button"))
            return false;

        return name.Contains("Panel") ||
               name.EndsWith("Screen", StringComparison.OrdinalIgnoreCase) ||
               name.EndsWith("Setting", StringComparison.OrdinalIgnoreCase) ||
               name.Contains("SettingRow");
    }

    private static void StripSelectableFeedback(Selectable selectable)
    {
        MenuSelectable menuSelectable = selectable.GetComponent<MenuSelectable>();
        if (menuSelectable != null)
            Object.DestroyImmediate(menuSelectable, true);

        MenuSelectableStateGraphics stateGraphics = selectable.GetComponent<MenuSelectableStateGraphics>();
        if (stateGraphics != null)
            Object.DestroyImmediate(stateGraphics, true);

        selectable.transition = Selectable.Transition.None;
        selectable.targetGraphic = null;
        EditorUtility.SetDirty(selectable);
    }

    private static MenuSelectionPersistence ResolvePersistence(Selectable selectable, string contextPath)
    {
        if (selectable is TMP_Dropdown or TMP_InputField or InputField)
            return MenuSelectionPersistence.WhileInteracting;

        if (contextPath.Contains("TrailColorButton") ||
            selectable.name.Contains("TrailColorButton") ||
            HasParentNamed(selectable.transform, "TrailColor"))
        {
            return MenuSelectionPersistence.Persistent;
        }

        return MenuSelectionPersistence.None;
    }

    private static MenuSelectableVisualPreset ResolveVisualPreset(Selectable selectable, string contextPath)
    {
        if (selectable.name.Contains("Back") || contextPath.Contains("BackButton"))
            return AssetDatabase.LoadAssetAtPath<MenuSelectableVisualPreset>(BackPresetPath);
        if (selectable.name.Contains("AddBot") || selectable.name.Contains("Add Bot"))
            return AssetDatabase.LoadAssetAtPath<MenuSelectableVisualPreset>(AddBotPresetPath);
        return AssetDatabase.LoadAssetAtPath<MenuSelectableVisualPreset>(DefaultPresetPath);
    }

    private static Graphic ResolveTargetGraphic(Selectable selectable)
    {
        if (IsUsableTargetGraphic(selectable.targetGraphic))
            return selectable.targetGraphic;

        Image ownImage = selectable.GetComponent<Image>();
        if (IsUsableTargetGraphic(ownImage))
            return ownImage;

        Graphic[] graphics = selectable.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            if (IsUsableTargetGraphic(graphic))
                return graphic;
        }

        return null;
    }

    private static bool IsUsableTargetGraphic(Graphic graphic)
    {
        if (graphic == null || graphic is TMP_Text)
            return false;

        string name = graphic.name;
        return !name.Contains("Text") &&
               !name.Contains("Icon") &&
               !name.Contains("Checkmark") &&
               !name.Contains("ColorPreview") &&
               !name.Contains("SelectionFrame") &&
               !name.Contains("Glow") &&
               !name.Contains("Overlay");
    }

    private static bool IsFullButton(Selectable selectable, string contextPath)
    {
        if (contextPath.EndsWith("MenuButton.prefab") ||
            contextPath.EndsWith("MenuBackButton.prefab") ||
            contextPath.EndsWith("MenuSelectorButton.prefab"))
        {
            return true;
        }

        RectTransform rect = selectable.transform as RectTransform;
        return rect != null &&
               rect.rect.width >= 220f &&
               rect.rect.height >= 48f &&
               selectable.name.Contains("Button") &&
               !HasParentNamed(selectable.transform, "MatchSettings") &&
               !HasParentNamed(selectable.transform, "ScooterSelection");
    }

    private static void PatchMainMenuPrefab(GameObject root)
    {
        ReplaceExactText(root, "SETTINGS", "settings");
        ReplaceExactText(root, "PLAYER STATS", "player stats");
        ReplaceExactText(root, "Exit", "exit");
        ReplaceExactText(root, "EXIT", "exit");
        RemoveIconsFromButtons(root, "settings");
        RemoveIconsFromButtons(root, "exit");
        PatchStatsScreen(root);
    }

    private static void PatchMainMenuButton(MainMenuController controller, string labelContains, UnityAction action, string newLabel, bool removeIcon)
    {
        Button button = FindButtonByLabel(labelContains);
        if (button == null)
            throw new InvalidOperationException($"MainMenu is missing button with label containing '{labelContains}'.");

        SetButtonLabel(button, newLabel);
        if (removeIcon)
            DisableButtonIcons(button.gameObject);

        ClearPersistentCalls(button);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void PatchMainMenuBackdrop()
    {
        foreach (Image image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!image.name.Contains("Background") && !image.name.Contains("Backdrop"))
                continue;

            image.color = new Color(0.018f, 0.045f, 0.062f, Mathf.Max(0.82f, image.color.a));
            EditorUtility.SetDirty(image);
        }
    }

    private static void PatchStatsScreen()
    {
        GameObject screen = FindGameObjectByName("PlayerStatsScreen");
        if (screen == null)
            return;

        Image screenImage = screen.GetComponent<Image>();
        if (screenImage == null)
            screenImage = screen.AddComponent<Image>();
        screenImage.color = new Color(0.012f, 0.028f, 0.036f, 0.72f);
        screenImage.raycastTarget = false;

        foreach (TMP_Text text in screen.GetComponentsInChildren<TMP_Text>(true))
        {
            string lower = text.name.ToLowerInvariant();
            if (lower.Contains("value"))
            {
                text.fontSize = Mathf.Max(text.fontSize, 34f);
                text.color = new Color(0.76f, 1f, 0.96f, 1f);
            }
            else if (lower.Contains("label") || lower.Contains("stat_"))
            {
                text.fontSize = Mathf.Max(text.fontSize, 20f);
                text.color = new Color(0.62f, 0.82f, 0.84f, 1f);
            }
        }

        foreach (Transform child in screen.GetComponentsInChildren<Transform>(true))
        {
            if (!child.name.Contains("Stat_"))
                continue;

            Image card = child.GetComponent<Image>();
            if (card == null)
                card = child.gameObject.AddComponent<Image>();
            card.color = new Color(0.035f, 0.12f, 0.14f, 0.76f);
            card.raycastTarget = false;
            EditorUtility.SetDirty(card);
        }
    }

    private static void PatchStatsScreen(GameObject root)
    {
        GameObject screen = root.name == "PlayerStatsScreen"
            ? root
            : FindChild(root.transform, "PlayerStatsScreen")?.gameObject;
        if (screen == null)
            return;

        Image screenImage = screen.GetComponent<Image>();
        if (screenImage == null)
            screenImage = screen.AddComponent<Image>();
        screenImage.color = new Color(0.012f, 0.028f, 0.036f, 0.72f);
        screenImage.raycastTarget = false;
        EditorUtility.SetDirty(screenImage);

        foreach (TMP_Text text in screen.GetComponentsInChildren<TMP_Text>(true))
        {
            string lower = text.name.ToLowerInvariant();
            if (lower.Contains("value"))
            {
                text.fontSize = Mathf.Max(text.fontSize, 34f);
                text.color = new Color(0.76f, 1f, 0.96f, 1f);
                EditorUtility.SetDirty(text);
            }
            else if (lower.Contains("label") || lower.Contains("stat_"))
            {
                text.fontSize = Mathf.Max(text.fontSize, 20f);
                text.color = new Color(0.62f, 0.82f, 0.84f, 1f);
                EditorUtility.SetDirty(text);
            }
        }
    }

    private static void PatchLobbyAtmosphere(GameObject root, bool multiplayer)
    {
        Camera camera = root.GetComponentInChildren<Camera>(true);
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = multiplayer
                ? new Color(0.012f, 0.018f, 0.062f, 1f)
                : new Color(0.018f, 0.038f, 0.055f, 1f);
            EditorUtility.SetDirty(camera);
        }

        Canvas canvas = root.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
            return;

        RectTransform background = EnsureRect(canvas.transform, "AtmosphereBackground");
        background.SetAsFirstSibling();
        Stretch(background);
        Image backgroundImage = background.GetComponent<Image>();
        if (backgroundImage == null)
            backgroundImage = background.gameObject.AddComponent<Image>();
        backgroundImage.color = multiplayer
            ? new Color(0.012f, 0.018f, 0.055f, 0.92f)
            : new Color(0.018f, 0.045f, 0.058f, 0.88f);
        backgroundImage.raycastTarget = false;

        RectTransform glow = EnsureRect(background, "AtmosphereHorizonGlow");
        glow.anchorMin = new Vector2(0f, 0.24f);
        glow.anchorMax = new Vector2(1f, 0.42f);
        glow.offsetMin = Vector2.zero;
        glow.offsetMax = Vector2.zero;
        Image glowImage = glow.GetComponent<Image>();
        if (glowImage == null)
            glowImage = glow.gameObject.AddComponent<Image>();
        glowImage.color = multiplayer
            ? new Color(0.1f, 0.22f, 0.42f, 0.24f)
            : new Color(0.08f, 0.34f, 0.32f, 0.22f);
        glowImage.raycastTarget = false;
    }

    private static void PatchLobbyPanelBrightness(GameObject root)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            string name = image.name;
            if (!name.Contains("Panel") && !name.Contains("Background") && !name.Contains("Backdrop"))
                continue;
            if (name.Contains("Atmosphere"))
                continue;

            Color color = image.color;
            if (color.r + color.g + color.b > 0.55f)
                continue;

            image.color = new Color(
                Mathf.Max(color.r, 0.035f),
                Mathf.Max(color.g, 0.075f),
                Mathf.Max(color.b, 0.09f),
                Mathf.Max(color.a, 0.66f));
            EditorUtility.SetDirty(image);
        }
    }

    private static void PatchMatchSettingsLabels(GameObject root)
    {
        ReplaceExactText(root, "MATCH DURATION", "Match duration");
        ReplaceExactText(root, "GAME MODE", "Game mode");
        ReplaceExactText(root, "SUDDEN DEATH", "Sudden death");
        ReplaceExactText(root, "TRAIL DURATION", "Trail duration");
        ReplaceExactText(root, "TRAIL LENGTH", "Trail duration");

        TMP_Text reference = FindText(root, "Match duration");
        TMP_Text trail = FindText(root, "Trail duration");
        if (reference != null && trail != null)
        {
            trail.font = reference.font;
            trail.fontSize = reference.fontSize;
            trail.color = reference.color;
            trail.alignment = reference.alignment;
        }
    }

    private static void PatchTrailColorLayout(GameObject root)
    {
        foreach (HorizontalLayoutGroup layout in root.GetComponentsInChildren<HorizontalLayoutGroup>(true))
        {
            if (!HasParentNamed(layout.transform, "TrailColor") && !layout.name.Contains("TrailColor"))
                continue;

            layout.spacing = 6f;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            EditorUtility.SetDirty(layout);
        }

        TMP_Text title = FindText(root, "TRAIL COLOR");
        if (title != null && title.transform.parent is RectTransform parent)
        {
            LayoutGroup layout = parent.GetComponent<LayoutGroup>();
            if (layout != null)
            {
                layout.padding.top = Mathf.Max(layout.padding.top, 10);
                layout.padding.bottom = Mathf.Max(layout.padding.bottom, 12);
                EditorUtility.SetDirty(layout);
            }
        }
    }

    private static void PatchMultiplayerConnectionBackground()
    {
        foreach (Image image in Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!image.name.Contains("Background") && !image.name.Contains("Backdrop"))
                continue;

            image.color = new Color(0.015f, 0.04f, 0.06f, Mathf.Max(0.86f, image.color.a));
            image.raycastTarget = false;
            EditorUtility.SetDirty(image);
        }
    }

    private static void PatchMultiplayerConnectionTitle()
    {
        TMP_Text title = FindSceneText("MULTIPLAYER");
        if (title == null)
            return;

        title.fontSize = Mathf.Max(title.fontSize, 58f);
        title.alignment = TextAlignmentOptions.Center;
        RectTransform rect = title.transform as RectTransform;
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -92f);
            rect.sizeDelta = new Vector2(560f, 80f);
        }

        if (title.transform.parent is RectTransform parent && parent.name.Contains("Title"))
        {
            parent.anchoredPosition = new Vector2(0f, -92f);
            parent.sizeDelta = new Vector2(620f, 96f);
        }
    }

    private static void PatchMultiplayerConnectionCards()
    {
        RectTransform host = FindRectByText("HOST SESSION");
        RectTransform join = FindRectByText("JOIN SESSION");
        if (host != null)
            ConfigureConnectionCard(host, new Vector2(-300f, -46f));
        if (join != null)
            ConfigureConnectionCard(join, new Vector2(300f, -46f));
    }

    private static void ConfigureConnectionCard(RectTransform rect, Vector2 anchoredPosition)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(440f, 300f);
    }

    private static void PatchMultiplayerConnectionBackButton()
    {
        Button back = FindButtonByMethod("BackToMainMenu") ?? FindButtonByLabel("back");
        if (back == null)
            return;

        RectTransform rect = back.transform as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 42f);
        rect.sizeDelta = new Vector2(260f, 64f);
        SetButtonLabel(back, "BACK");
    }

    private static void VerifyNoMissingScripts()
    {
        foreach (MonoBehaviour behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
        {
            if (behaviour == null)
                throw new InvalidOperationException("A loaded scene or prefab contains a missing script.");
        }
    }

    private static void VerifyMainMenuActions()
    {
        EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        RequireButtonMethod("settings", "ShowSettings", "ShowOptions");
        RequireButtonMethod("exit", "Exit", null);
        RequireButtonMethod("player stats", "ShowStatisticsScreen", null);
    }

    private static void VerifySelectionPolicies()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project/Prefabs/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            foreach (MenuSelectable selectable in prefab.GetComponentsInChildren<MenuSelectable>(true))
            {
                Selectable unitySelectable = selectable.GetComponent<Selectable>();
                MenuSelectionPersistence expected = ResolvePersistence(unitySelectable, path);
                if (selectable.SelectionPersistence != expected)
                    throw new InvalidOperationException($"{path}/{selectable.name} has {selectable.SelectionPersistence} selection persistence; expected {expected}.");

                if (IsContainerSelectable(unitySelectable))
                    throw new InvalidOperationException($"{path}/{selectable.name} is a container with MenuSelectable still attached.");
            }
        }
    }

    private static void VerifyLobbyAtmosphere()
    {
        VerifyLobbyAtmosphere(SingleplayerLobbyRootPrefabPath);
        VerifyLobbyAtmosphere(MultiplayerLobbyRootPrefabPath);
    }

    private static void VerifyLobbyAtmosphere(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException($"{path} is missing.");
        if (FindChild(prefab.transform, "AtmosphereBackground") == null)
            throw new InvalidOperationException($"{path} is missing AtmosphereBackground.");
        Camera camera = prefab.GetComponentInChildren<Camera>(true);
        if (camera == null || camera.backgroundColor.grayscale <= 0.01f)
            throw new InvalidOperationException($"{path} has no visible non-black camera background.");
    }

    private static void VerifyMultiplayerConnection()
    {
        EditorSceneManager.OpenScene(MultiplayerConnectionScenePath, OpenSceneMode.Single);
        TMP_Text title = FindSceneText("MULTIPLAYER");
        if (title == null || title.fontSize < 54f)
            throw new InvalidOperationException("MultiplayerConnection title is missing or too small.");
        Button back = FindButtonByMethod("BackToMainMenu") ?? FindButtonByLabel("back");
        RectTransform rect = back != null ? back.transform as RectTransform : null;
        if (rect == null || rect.sizeDelta.x < 240f || rect.sizeDelta.y < 58f)
            throw new InvalidOperationException("MultiplayerConnection back button is missing or too small.");
    }

    private static void VerifyStatsScreen()
    {
        EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        GameObject screen = FindGameObjectByName("PlayerStatsScreen");
        if (screen == null)
            throw new InvalidOperationException("PlayerStatsScreen is missing.");
        if (FindGameObjectByName("StatisticsScreen") != null)
            throw new InvalidOperationException("Old StatisticsScreen object still exists.");
        if (screen.GetComponentsInChildren<Image>(true).Length < 2)
            throw new InvalidOperationException("PlayerStatsScreen does not contain stat card/row background images.");
    }

    private static void RequireButtonMethod(string labelContains, string methodName, string alternateMethodName)
    {
        Button button = FindButtonByLabel(labelContains);
        if (button == null)
            throw new InvalidOperationException($"Missing main menu button for '{labelContains}'.");

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            string persistentMethod = button.onClick.GetPersistentMethodName(i);
            if (persistentMethod == methodName || persistentMethod == alternateMethodName)
                return;
        }

        throw new InvalidOperationException($"Main menu '{labelContains}' button is not wired to {methodName}.");
    }

    private static Button FindButtonByLabel(string labelContains)
    {
        string needle = labelContains.ToLowerInvariant();
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (TMP_Text text in button.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.text.ToLowerInvariant().Contains(needle))
                    return button;
            }
        }

        return null;
    }

    private static Button FindButtonByMethod(string methodName)
    {
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentMethodName(i) == methodName)
                    return button;
            }
        }

        return null;
    }

    private static RectTransform FindRectByText(string value)
    {
        TMP_Text text = FindSceneText(value);
        return text != null ? text.GetComponentInParent<RectTransform>() : null;
    }

    private static TMP_Text FindSceneText(string value)
    {
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (string.Equals(text.text, value, StringComparison.OrdinalIgnoreCase))
                return text;
        }

        return null;
    }

    private static TMP_Text FindText(GameObject root, string value)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (string.Equals(text.text, value, StringComparison.OrdinalIgnoreCase))
                return text;
        }

        return null;
    }

    private static void ReplaceExactText(GameObject root, string oldValue, string newValue)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.text == oldValue)
            {
                text.text = newValue;
                EditorUtility.SetDirty(text);
            }
        }
    }

    private static void SetButtonLabel(Button button, string label)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
            EditorUtility.SetDirty(text);
        }
    }

    private static void ClearPersistentCalls(Button button)
    {
        SerializedObject serialized = new(button);
        SerializedProperty calls = serialized.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
        if (calls != null)
        {
            calls.ClearArray();
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void RemoveIconsFromButtons(GameObject root, string labelContains)
    {
        foreach (Button button in root.GetComponentsInChildren<Button>(true))
        {
            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
            if (text != null && text.text.ToLowerInvariant().Contains(labelContains))
                DisableButtonIcons(button.gameObject);
        }
    }

    private static void DisableButtonIcons(GameObject button)
    {
        foreach (Image image in button.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject == button)
                continue;
            if (image.name.Contains("Icon"))
            {
                image.gameObject.SetActive(false);
                EditorUtility.SetDirty(image.gameObject);
            }
        }
    }

    private static RectTransform EnsureRect(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing as RectTransform;

        GameObject created = new(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
        RectTransform rect = created.transform as RectTransform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static GameObject FindGameObjectByName(string name)
    {
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform.name == name)
                return transform.gameObject;
        }

        return null;
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (root.name == name)
            return root;
        foreach (Transform child in root)
        {
            Transform found = FindChild(child, name);
            if (found != null)
                return found;
        }

        return null;
    }

    private static bool HasParentNamed(Transform transform, string namePart)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name.Contains(namePart))
                return true;
            current = current.parent;
        }

        return false;
    }

    private static void SetObject(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetEnum(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.enumValueIndex = value;
    }

    private static void SetBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
            property.boolValue = value;
    }

    private static void EnsureFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
    }
}
