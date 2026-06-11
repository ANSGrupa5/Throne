using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class Milestone82TargetedUiRepair
{
    private const string MainMenuScenePath = "Assets/Project/Scenes/UI/Menu/MainMenu.unity";
    private const string MultiplayerConnectionScenePath = "Assets/Project/Scenes/UI/Menu/MultiplayerConnection.unity";
    private const string LobbyScenePath = "Assets/Project/Scenes/UI/Lobby/LobbyScene.unity";
    private const string GameOverScenePath = "Assets/Project/Scenes/UI/Menu/GameOver.unity";

    private const string MenuButtonPrefabPath = "Assets/Project/Prefabs/UI/Menu/MenuButton.prefab";
    private const string MenuBackButtonPrefabPath = "Assets/Project/Prefabs/UI/Menu/MenuBackButton.prefab";
    private const string MenuSelectorButtonPrefabPath = "Assets/Project/Prefabs/UI/Menu/MenuSelectorButton.prefab";
    private const string TrailColorButtonPrefabPath = "Assets/Project/Prefabs/UI/Lobby/TrailColorButton.prefab";
    private const string MatchSettingsPanelPrefabPath = "Assets/Project/Prefabs/UI/Lobby/Panels/MatchSettingsPanel.prefab";
    private const string SingleplayerLobbyRootPrefabPath = "Assets/Project/Prefabs/UI/Lobby/SingleplayerLobbyRoot.prefab";
    private const string MultiplayerLobbyRootPrefabPath = "Assets/Project/Prefabs/UI/Lobby/MultiplayerLobbyRoot.prefab";

    private const string MainScreenPrefabPath = "Assets/Project/Prefabs/UI/Menu/Screens/MainScreen.prefab";
    private const string MainSettingsScreenPrefabPath = "Assets/Project/Prefabs/UI/Menu/Screens/MainMenuSettingsScreen.prefab";
    private const string SoundSettingsScreenPrefabPath = "Assets/Project/Prefabs/UI/Menu/Screens/SoundSettingsScreen.prefab";
    private const string GraphicsSettingsScreenPrefabPath = "Assets/Project/Prefabs/UI/Menu/Screens/GraphicsSettingsScreen.prefab";
    private const string ControlsSettingsScreenPrefabPath = "Assets/Project/Prefabs/UI/Menu/Screens/ControlsSettingsScreen.prefab";
    private const string PlayerStatsScreenPrefabPath = "Assets/Project/Prefabs/UI/Menu/Screens/PlayerStatsScreen.prefab";
    private const string PlayerStatCardPrefabPath = "Assets/Project/Prefabs/UI/Menu/Rows/PlayerStatCard.prefab";

    private const string HostSessionCardPrefabPath = "Assets/Project/Prefabs/UI/Menu/Multiplayer/HostSessionCard.prefab";
    private const string JoinSessionCardPrefabPath = "Assets/Project/Prefabs/UI/Menu/Multiplayer/JoinSessionCard.prefab";
    private const string AddressInputPanelPrefabPath = "Assets/Project/Prefabs/UI/Menu/Multiplayer/AddressInputPanel.prefab";
    private const string ConnectionPopupPrefabPath = "Assets/Project/Prefabs/UI/Menu/Multiplayer/ConnectionPopup.prefab";

    private const string DefaultPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Default.asset";
    private const string BackPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Back.asset";
    private const string DarkerPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Darker.asset";
    private const string TransparentPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Transparent.asset";
    private const string DefaultAudioPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuAudio_Default.asset";

    private static readonly Dictionary<string, string> RequiredMainMenuLabels = new()
    {
        { "SingleplayerButton", "Singleplayer" },
        { "MultiplayerButton", "Multiplayer" },
        { "StatisticsButton", "Player stats" },
        { "OptionsButton", "Settings" },
        { "ExitButton", "Exit" }
    };

    [MenuItem("Throne/Tools/Migrations/Milestone 8.2/Run Targeted UI Repair")]
    public static void RunTargetedRepair()
    {
        ConfigureVisualPresets();
        PatchCoreButtonPrefabs();
        PatchTrailColorButtonPrefab();
        EnsurePlayerStatCardPrefab();
        PatchMenuScreenPrefabs();
        PatchLobbyPrefabs();
        PatchMultiplayerConnectionPrefabs();

        PatchMainMenuScene();
        PatchMultiplayerConnectionScene();
        PatchLobbyScene();
        PatchGameOverScene();
        CleanStaleRenameMetadata();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Milestone82TargetedUiRepair] Targeted repair complete.");
    }

    [MenuItem("Throne/Tools/Migrations/Milestone 8.2/Verify Targeted UI Repair")]
    public static void Verify()
    {
        List<string> failures = new();

        VerifyNoMissingScripts(failures);
        VerifyMainMenu(failures);
        VerifySettingsPrefabs(failures);
        VerifyLobbyPrefabs(failures);
        VerifyTrailColorButton(failures);
        VerifyPlayerStatsPrefab(failures);
        VerifyMultiplayerConnection(failures);
        VerifyStaleMetadata(failures);

        if (failures.Count > 0)
            throw new InvalidOperationException("[Milestone82TargetedUiRepair] Verification failed:\n" + string.Join("\n", failures));

        Debug.Log("[Milestone82TargetedUiRepair] Verification complete.");
    }

    private static void ConfigureVisualPresets()
    {
        ConfigurePreset(DefaultPresetPath,
            new Color(0.08f, 0.62f, 0.70f, 0.58f),
            new Color(0.20f, 0.94f, 1.00f, 0.82f),
            new Color(0.03f, 0.48f, 0.58f, 0.78f),
            new Color(0.12f, 0.78f, 0.88f, 0.78f),
            new Color(0.06f, 0.10f, 0.12f, 0.32f));

        ConfigurePreset(BackPresetPath,
            new Color(0.18f, 0.22f, 0.27f, 0.48f),
            new Color(0.34f, 0.40f, 0.46f, 0.64f),
            new Color(0.12f, 0.16f, 0.20f, 0.70f),
            new Color(0.24f, 0.30f, 0.36f, 0.62f),
            new Color(0.08f, 0.10f, 0.12f, 0.28f));

        ConfigurePreset(DarkerPresetPath,
            new Color(0.08f, 0.18f, 0.21f, 0.58f),
            new Color(0.16f, 0.42f, 0.48f, 0.72f),
            new Color(0.06f, 0.26f, 0.30f, 0.76f),
            new Color(0.12f, 0.34f, 0.39f, 0.70f),
            new Color(0.05f, 0.08f, 0.09f, 0.32f));
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

    private static void PatchCoreButtonPrefabs()
    {
        MenuSelectableVisualPreset defaultPreset = LoadAsset<MenuSelectableVisualPreset>(DefaultPresetPath);
        MenuSelectableVisualPreset backPreset = LoadAsset<MenuSelectableVisualPreset>(BackPresetPath);
        MenuSelectableAudioPreset defaultAudio = LoadAsset<MenuSelectableAudioPreset>(DefaultAudioPresetPath);

        PatchPrefab(MenuButtonPrefabPath, root =>
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220f, 40f);
            Image image = EnsureComponent<Image>(root);
            image.color = defaultPreset.NormalColor;
            image.raycastTarget = true;
            ConfigureSelectable(root, defaultPreset, defaultAudio, image, true, MenuSelectionPersistence.None, true);
            ConfigureStateGraphics(root, new Color(0f, 0.95f, 1f, 0.16f));
            SetButtonText(root.GetComponent<Button>(), FirstButtonText(root) ?? "Button");
            SetChildActive(root.transform, "ButtonIcon", false);
        });

        PatchPrefab(MenuBackButtonPrefabPath, root =>
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(240f, 44f);
            Image image = EnsureComponent<Image>(root);
            image.color = backPreset.NormalColor;
            image.raycastTarget = true;
            ConfigureSelectable(root, backPreset, defaultAudio, image, true, MenuSelectionPersistence.None, true);
            ConfigureStateGraphics(root, new Color(0.70f, 0.86f, 0.94f, 0.10f));
            SetChildActive(root.transform, "Back Icon", false);
            SetChildActive(root.transform, "ButtonIcon", false);
            foreach (Outline outline in root.GetComponentsInChildren<Outline>(true))
                outline.effectColor = new Color(0.60f, 0.72f, 0.78f, 0.25f);
        });

        PatchPrefab(MenuSelectorButtonPrefabPath, root =>
        {
            Image image = EnsureComponent<Image>(root);
            image.color = defaultPreset.NormalColor;
            ConfigureSelectable(root, defaultPreset, defaultAudio, image, true, MenuSelectionPersistence.None, true);
            ConfigureStateGraphics(root, new Color(0f, 0.95f, 1f, 0.12f));
        });
    }

    private static void PatchTrailColorButtonPrefab()
    {
        MenuSelectableVisualPreset transparentPreset = LoadAsset<MenuSelectableVisualPreset>(TransparentPresetPath, false);
        MenuSelectableAudioPreset defaultAudio = LoadAsset<MenuSelectableAudioPreset>(DefaultAudioPresetPath);

        PatchPrefab(TrailColorButtonPrefabPath, root =>
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(25f, 25f);

            Image rootImage = EnsureComponent<Image>(root);
            Sprite oldSprite = rootImage.sprite;
            Material oldMaterial = rootImage.material;
            Color oldColor = rootImage.color;
            rootImage.sprite = null;
            rootImage.material = null;
            rootImage.color = new Color(1f, 1f, 1f, 0.02f);
            rootImage.raycastTarget = true;

            Image preview = EnsureChildImage(root.transform, "ColorPreview");
            preview.sprite = oldSprite;
            preview.material = oldMaterial;
            preview.color = new Color(oldColor.r, oldColor.g, oldColor.b, 1f);
            preview.raycastTarget = false;
            Stretch(preview.rectTransform, new Vector2(-3f, -3f));
            preview.transform.SetAsFirstSibling();

            GameObject frameObject = EnsureChild(root.transform, "SelectionFrame", typeof(RectTransform), typeof(Image), typeof(Outline));
            frameObject.SetActive(false);
            Image frameImage = frameObject.GetComponent<Image>();
            frameImage.color = new Color(0f, 0.996f, 0.925f, 0.16f);
            frameImage.raycastTarget = false;
            Outline frameOutline = frameObject.GetComponent<Outline>();
            frameOutline.effectColor = new Color(0f, 0.996f, 0.925f, 1f);
            frameOutline.effectDistance = new Vector2(2f, -2f);
            Stretch(frameObject.GetComponent<RectTransform>(), new Vector2(6f, 6f));

            CanvasGroup group = EnsureComponent<CanvasGroup>(root);
            group.alpha = 1f;

            Button button = EnsureComponent<Button>(root);
            button.targetGraphic = rootImage;
            button.transition = Selectable.Transition.None;

            ConfigureSelectable(root, transparentPreset, defaultAudio, rootImage, false, MenuSelectionPersistence.Persistent, false);
            ConfigureStateGraphics(root, new Color(0f, 0.95f, 1f, 0.08f), frameObject.GetComponent<Graphic>());
        });
    }

    private static void EnsurePlayerStatCardPrefab()
    {
        EnsureFolder("Assets/Project/Prefabs/UI/Menu/Rows");
        GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerStatCardPrefabPath);
        bool loadedExisting = root != null;
        GameObject contents = loadedExisting ? PrefabUtility.LoadPrefabContents(PlayerStatCardPrefabPath) : CreateStatCardRoot();

        NormalizeStatCard(contents);
        PrefabUtility.SaveAsPrefabAsset(contents, PlayerStatCardPrefabPath);
        if (loadedExisting)
            PrefabUtility.UnloadPrefabContents(contents);
        else
            Object.DestroyImmediate(contents);
    }

    private static GameObject CreateStatCardRoot()
    {
        GameObject root = new("PlayerStatCard", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(VerticalLayoutGroup));
        CreateTmpChild(root.transform, "StatLabelText");
        CreateTmpChild(root.transform, "StatValueText");
        return root;
    }

    private static void NormalizeStatCard(GameObject root)
    {
        root.name = "PlayerStatCard";
        RectTransform rect = EnsureComponent<RectTransform>(root);
        rect.sizeDelta = new Vector2(190f, 86f);

        Image image = EnsureComponent<Image>(root);
        image.color = new Color(0.025f, 0.20f, 0.24f, 0.46f);
        image.raycastTarget = false;

        Outline outline = EnsureComponent<Outline>(root);
        outline.effectColor = new Color(0f, 0.9f, 1f, 0.28f);
        outline.effectDistance = new Vector2(1f, -1f);

        VerticalLayoutGroup layout = EnsureComponent<VerticalLayoutGroup>(root);
        layout.padding = new RectOffset(14, 14, 10, 10);
        layout.spacing = 4f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text label = EnsureTmpChild(root.transform, "StatLabelText");
        label.fontSize = 17f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.74f, 0.94f, 0.96f, 0.90f);
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text value = EnsureTmpChild(root.transform, "StatValueText");
        value.fontSize = 27f;
        value.alignment = TextAlignmentOptions.Center;
        value.color = new Color(0.94f, 1f, 0.98f, 1f);
        value.textWrappingMode = TextWrappingModes.NoWrap;
        value.overflowMode = TextOverflowModes.Overflow;
    }

    private static void PatchMenuScreenPrefabs()
    {
        PatchPrefab(MainScreenPrefabPath, PatchMainScreen);
        PatchPrefab(MainSettingsScreenPrefabPath, root =>
        {
            PatchScreenTitle(root, "OptionsMenuText (TMP)", "SETTINGS");
            PatchSettingsScreenButtons(root);
        });
        PatchPrefab(SoundSettingsScreenPrefabPath, root => PatchScreenTitle(root, "SoundMenuText (TMP)", "SOUND SETTINGS"));
        PatchPrefab(GraphicsSettingsScreenPrefabPath, root => PatchScreenTitle(root, "GraphicsMenuText (TMP)", "GRAPHICS SETTINGS"));
        PatchPrefab(ControlsSettingsScreenPrefabPath, root => PatchScreenTitle(root, "KeybindsMenuText (TMP)", "KEYBINDS SETTINGS"));
        PatchPrefab(PlayerStatsScreenPrefabPath, PatchPlayerStatsScreen);
    }

    private static void PatchMainScreen(GameObject root)
    {
        foreach ((string buttonName, string label) in RequiredMainMenuLabels)
        {
            Button button = FindDeepChild(root.transform, buttonName)?.GetComponentInChildren<Button>(true);
            if (button == null)
                continue;

            SetButtonText(button, label);
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(220f, 40f);
        }

        DisableButtonIcon(root.transform, "OptionsButton");
        DisableButtonIcon(root.transform, "ExitButton");
    }

    private static void PatchSettingsScreenButtons(GameObject root)
    {
        SetButtonText(FindDeepChild(root.transform, "GraphicsSettingsButton")?.GetComponentInChildren<Button>(true), "Graphics");
        SetButtonText(FindDeepChild(root.transform, "SoundSettingsButton")?.GetComponentInChildren<Button>(true), "Sound");
        SetButtonText(FindDeepChild(root.transform, "KeybindsSettingsButton")?.GetComponentInChildren<Button>(true), "Controls");
    }

    private static void PatchScreenTitle(GameObject root, string titleObjectName, string title)
    {
        TMP_Text titleText = FindDeepChild(root.transform, titleObjectName)?.GetComponent<TMP_Text>();
        if (titleText == null)
            titleText = root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => text.name.Contains("MenuText") || text.name.Contains("Title"));
        if (titleText == null)
            return;

        titleText.text = title;
        titleText.fontSize = Mathf.Max(titleText.fontSize, 30f);
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Overflow;
        titleText.color = new Color(0.92f, 1f, 0.98f, 1f);

        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(520f, Mathf.Max(52f, titleRect.sizeDelta.y));

        EnsureTitleFrame(titleText.transform.parent, titleRect);
    }

    private static void EnsureTitleFrame(Transform parent, RectTransform titleRect)
    {
        GameObject frame = EnsureChild(parent, "TitleFrame", typeof(RectTransform), typeof(Image), typeof(Outline));
        RectTransform frameRect = frame.GetComponent<RectTransform>();
        frameRect.anchorMin = titleRect.anchorMin;
        frameRect.anchorMax = titleRect.anchorMax;
        frameRect.pivot = titleRect.pivot;
        frameRect.anchoredPosition = titleRect.anchoredPosition;
        frameRect.sizeDelta = new Vector2(Mathf.Max(460f, titleRect.sizeDelta.x + 34f), 54f);
        frame.transform.SetSiblingIndex(Mathf.Max(0, titleRect.GetSiblingIndex()));
        titleRect.transform.SetAsLastSibling();

        Image image = frame.GetComponent<Image>();
        image.color = new Color(0.02f, 0.22f, 0.28f, 0.42f);
        image.raycastTarget = false;

        Outline outline = frame.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0.9f, 1f, 0.26f);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private static void PatchPlayerStatsScreen(GameObject root)
    {
        PatchScreenTitle(root, "StatisticsMenuText (TMP)", "PLAYER STATS");

        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = root.transform.GetChild(i);
            if (child.name.StartsWith("StatsRow", StringComparison.Ordinal) ||
                child.name.StartsWith("Stat_", StringComparison.Ordinal) ||
                child.name == "StatsPanel")
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        GameObject panel = EnsureChild(root.transform, "StatsPanel", typeof(RectTransform), typeof(Image), typeof(Outline));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -64f);
        panelRect.sizeDelta = new Vector2(650f, 230f);
        panel.GetComponent<Image>().color = new Color(0.012f, 0.075f, 0.095f, 0.36f);
        panel.GetComponent<Image>().raycastTarget = false;
        panel.GetComponent<Outline>().effectColor = new Color(0f, 0.8f, 1f, 0.18f);

        GameObject grid = EnsureChild(panel.transform, "StatsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
        RectTransform gridRect = grid.GetComponent<RectTransform>();
        Stretch(gridRect, new Vector2(-18f, -18f));
        GridLayoutGroup layout = grid.GetComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(190f, 86f);
        layout.spacing = new Vector2(16f, 16f);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        layout.childAlignment = TextAnchor.MiddleCenter;

        GameObject cardPrefab = LoadAsset<GameObject>(PlayerStatCardPrefabPath);
        CreateStatCard(grid.transform, cardPrefab, "StatOppsElimCard", "Opponents eliminated", "StatOppsElimValueText", "0");
        CreateStatCard(grid.transform, cardPrefab, "StatTimesElimCard", "Times eliminated", "StatTimesElimValueText", "0");
        CreateStatCard(grid.transform, cardPrefab, "StatTotalPowerUpsCard", "Powerups picked up", "StatTotalPowValueText", "0");
        CreateStatCard(grid.transform, cardPrefab, "StatWinsCard", "Wins", "StatWinsValueText", "0");
        CreateStatCard(grid.transform, cardPrefab, "StatLossesCard", "Losses", "StatLossesValueText", "0");
        CreateStatCard(grid.transform, cardPrefab, "StatDistanceDrivenCard", "Distance driven", "StatDistDrivenValueText", "0.00 km");
    }

    private static void CreateStatCard(Transform parent, GameObject prefab, string cardName, string label, string valueName, string value)
    {
        Transform existing = FindDeepChild(parent, cardName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject card = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        card.name = cardName;

        TMP_Text labelText = FindDeepChild(card.transform, "StatLabelText")?.GetComponent<TMP_Text>();
        if (labelText != null)
            labelText.text = label;

        TMP_Text valueText = FindDeepChild(card.transform, "StatValueText")?.GetComponent<TMP_Text>();
        if (valueText != null)
        {
            valueText.name = valueName;
            valueText.text = value;
        }
    }

    private static void PatchLobbyPrefabs()
    {
        PatchPrefab(MatchSettingsPanelPrefabPath, PatchMatchSettingsLabels);
        PatchPrefab(SingleplayerLobbyRootPrefabPath, root => PatchLobbyRoot(root, "SINGLEPLAYER", LobbyMode.Singleplayer));
        PatchPrefab(MultiplayerLobbyRootPrefabPath, root => PatchLobbyRoot(root, "MULTIPLAYER", LobbyMode.MultiplayerClient));
    }

    private static void PatchLobbyRoot(GameObject root, string title, LobbyMode mode)
    {
        LobbyController controller = root.GetComponentInChildren<LobbyController>(true);
        if (controller != null)
        {
            SerializedObject serialized = new(controller);
            SetEnum(serialized, "configuredLobbyMode", (int)mode);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PatchLobbyTrailColorReferences(controller);
        }

        TMP_Text lobbyTitle = FindDeepChild(root.transform, "LobbyText (TMP)")?.GetComponent<TMP_Text>();
        if (lobbyTitle != null)
        {
            lobbyTitle.text = title;
            lobbyTitle.textWrappingMode = TextWrappingModes.NoWrap;
            lobbyTitle.overflowMode = TextOverflowModes.Overflow;
        }

        foreach (string overlayName in new[] { "AtmosphereBackground", "AtmosphereHorizonGlow", "ReadabilityScrim" })
            SetChildActive(root.transform, overlayName, false);

        foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
        {
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.backgroundColor = new Color(0.54f, 0.78f, 0.98f, 1f);
        }

        foreach (Light light in root.GetComponentsInChildren<Light>(true))
        {
            if (light.type == LightType.Directional)
            {
                light.color = new Color(1f, 0.94f, 0.78f, 1f);
                light.intensity = 1.15f;
                light.transform.rotation = Quaternion.Euler(48f, -34f, 0f);
            }
            else
            {
                light.color = Color.Lerp(light.color, new Color(0.55f, 0.95f, 1f, 1f), 0.35f);
                light.intensity = Mathf.Max(light.intensity * 0.75f, 0.65f);
            }
        }

        PatchMatchSettingsLabels(root);
        BrightenLobbyPanels(root);
    }

    private static void BrightenLobbyPanels(GameObject root)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            string name = image.gameObject.name;
            if (name.Contains("Atmosphere") || name.Contains("Readability") ||
                name.Contains("ColorPreview") || name.Contains("SelectionFrame") ||
                name.Contains("HoverGlow") || name.Contains("DisabledOverlay"))
            {
                continue;
            }

            if (!name.Contains("Panel") && !name.Contains("Background") && !name.Contains("Frame") && !name.Contains("Backplate"))
                continue;

            Color color = image.color;
            color.r = Mathf.Max(color.r, 0.025f);
            color.g = Mathf.Max(color.g, 0.18f);
            color.b = Mathf.Max(color.b, 0.22f);
            color.a = Mathf.Min(Mathf.Max(color.a, 0.30f), 0.58f);
            image.color = color;
        }
    }

    private static void PatchMatchSettingsLabels(GameObject root)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            string normalized = NormalizeText(text.text);
            switch (normalized)
            {
                case "MATCH DURATION":
                    StyleSettingLabel(text, "Match duration");
                    break;
                case "GAME MODE":
                    StyleSettingLabel(text, "Game mode");
                    break;
                case "SUDDEN DEATH":
                    StyleSettingLabel(text, "Sudden death");
                    break;
                case "TRAIL DURATION":
                case "TRAIL LENGTH":
                    StyleSettingLabel(text, "Trail duration");
                    break;
            }
        }

        EnsureGameModeLabel(root);
    }

    private static void EnsureGameModeLabel(GameObject root)
    {
        if (root.GetComponentsInChildren<TMP_Text>(true).Any(text => NormalizeText(text.text) == "GAME MODE"))
            return;

        Transform selector = FindDeepChild(root.transform, "GameModeSelector");
        if (selector == null || selector.parent == null)
            return;

        GameObject labelObject = EnsureChild(selector.parent, "GameModeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        StyleSettingLabel(label, "Game mode");

        RectTransform selectorRect = selector.GetComponent<RectTransform>();
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = selectorRect != null ? selectorRect.anchorMin : new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = selectorRect != null ? selectorRect.anchorMax : new Vector2(0.5f, 0.5f);
        labelRect.pivot = selectorRect != null ? selectorRect.pivot : new Vector2(0.5f, 0.5f);
        labelRect.sizeDelta = new Vector2(180f, 24f);
        labelRect.anchoredPosition = selectorRect != null
            ? selectorRect.anchoredPosition + new Vector2(0f, 34f)
            : Vector2.zero;
    }

    private static void StyleSettingLabel(TMP_Text text, string value)
    {
        text.text = value;
        text.fontSize = 18f;
        text.alignment = TextAlignmentOptions.Left;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = new Color(0.82f, 0.98f, 0.96f, 0.95f);
    }

    private static void PatchLobbyTrailColorReferences(LobbyController controller)
    {
        SerializedObject serialized = new(controller);
        SerializedProperty buttons = serialized.FindProperty("trailColorButtons");
        if (buttons == null || !buttons.isArray)
            return;

        Button[] colorButtons = controller.GetComponentsInChildren<Button>(true)
            .Where(button => button.name == "TrailColorButton")
            .OrderBy(button => button.transform.position.x)
            .ToArray();

        buttons.arraySize = colorButtons.Length;
        for (int i = 0; i < colorButtons.Length; i++)
        {
            Button button = colorButtons[i];
            Image preview = FindDeepChild(button.transform, "ColorPreview")?.GetComponent<Image>();
            GameObject frame = FindDeepChild(button.transform, "SelectionFrame")?.gameObject;
            CanvasGroup group = button.GetComponent<CanvasGroup>();

            SerializedProperty entry = buttons.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("button").objectReferenceValue = button;
            entry.FindPropertyRelative("colorImage").objectReferenceValue = preview;
            entry.FindPropertyRelative("selectionFrame").objectReferenceValue = frame;
            entry.FindPropertyRelative("availabilityGroup").objectReferenceValue = group;
            entry.FindPropertyRelative("availableAlpha").floatValue = 0.90f;
            entry.FindPropertyRelative("selectedAlpha").floatValue = 1.0f;
            entry.FindPropertyRelative("unavailableAlpha").floatValue = 0.35f;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PatchMultiplayerConnectionPrefabs()
    {
        PatchConnectionCardPrefab(HostSessionCardPrefabPath, "HOST SESSION", "Create a local network arena and wait for players.", "HOST");
        PatchConnectionCardPrefab(JoinSessionCardPrefabPath, "JOIN SESSION", "Connect to an existing host.", "JOIN");

        PatchPrefab(AddressInputPanelPrefabPath, root =>
        {
            SetTextByName(root, "JoinTitle", "JOIN GAME", 34f);
            SetTextByName(root, "AddressLabel", "ENTER HOST ADDRESS", 22f);
            TMP_InputField input = root.GetComponentInChildren<TMP_InputField>(true);
            if (input != null)
            {
                RectTransform rect = input.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(430f, 56f);
                Image image = EnsureComponent<Image>(input.gameObject);
                image.color = new Color(0.02f, 0.18f, 0.22f, 0.62f);
                Outline outline = EnsureComponent<Outline>(input.gameObject);
                outline.effectColor = new Color(0f, 0.9f, 1f, 0.38f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
        });

        PatchPrefab(ConnectionPopupPrefabPath, root =>
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(560f, 300f);
            Image image = EnsureComponent<Image>(root);
            image.color = new Color(0.025f, 0.055f, 0.075f, 0.92f);
        });
    }

    private static void PatchConnectionCardPrefab(string path, string title, string body, string buttonText)
    {
        PatchPrefab(path, root =>
        {
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(480f, 300f);

            Image image = EnsureComponent<Image>(root);
            image.color = new Color(0.025f, 0.19f, 0.23f, 0.48f);
            Outline outline = EnsureComponent<Outline>(root);
            outline.effectColor = new Color(0f, 0.9f, 1f, 0.30f);
            outline.effectDistance = new Vector2(1f, -1f);

            SetTextByName(root, "Title", title, 34f);
            SetTextByName(root, "Body", body, 19f);
            Button button = root.GetComponentInChildren<Button>(true);
            SetButtonText(button, buttonText);
        });
    }

    private static void PatchMainMenuScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (controller == null)
            throw new InvalidOperationException("MainMenu scene is missing MainMenuController.");

        Transform root = controller.transform.root;
        PatchMainScreen(root.gameObject);
        PatchSceneScreenTitles(root);
        PatchSceneMainMenuEvents(root, controller);
        PatchPlayerProfileStatsReferences();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchSceneMainMenuEvents(Transform root, MainMenuController controller)
    {
        SetButtonEvent(root, "SingleplayerButton", "Singleplayer", controller.LoadSingleplayer);
        SetButtonEvent(root, "MultiplayerButton", "Multiplayer", controller.LoadMultiplayerConnection);
        SetButtonEvent(root, "OptionsButton", "Settings", controller.ShowSettings);
        SetButtonEvent(root, "StatisticsButton", "Player stats", controller.ShowStatisticsScreen);
        SetButtonEvent(root, "ExitButton", "Exit", controller.Exit);

        SetButtonEvent(root, "GraphicsSettingsButton", "Graphics", controller.ShowGraphicsSettings);
        SetButtonEvent(root, "SoundSettingsButton", "Sound", controller.ShowSoundSettings);
        SetButtonEvent(root, "KeybindsSettingsButton", "Controls", controller.ShowKeybindsSettings);

        SetBackButtonForScreen(root, "MainMenuSettingsScreen", controller.GoBackToMainScreen);
        SetBackButtonForScreen(root, "SoundSettingsScreen", controller.GoBackToSettingsScreen);
        SetBackButtonForScreen(root, "GraphicsSettingsScreen", controller.GoBackToSettingsScreen);
        SetBackButtonForScreen(root, "ControlsSettingsScreen", controller.GoBackToSettingsScreen);
        SetBackButtonForScreen(root, "PlayerStatsScreen", controller.GoBackToSettingsScreen);
    }

    private static void SetButtonEvent(Transform root, string buttonObjectName, string label, UnityAction action)
    {
        Button button = FindDeepChild(root, buttonObjectName)?.GetComponentInChildren<Button>(true);
        if (button == null)
            return;

        SetButtonText(button, label);
        ClearPersistent(button.onClick);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);

        if (buttonObjectName == "OptionsButton" || buttonObjectName == "ExitButton")
            DisableButtonIcon(button.transform);
    }

    private static void SetBackButtonForScreen(Transform root, string screenName, UnityAction action)
    {
        Transform screen = FindDeepChild(root, screenName);
        if (screen == null)
            return;

        Button back = screen.GetComponentsInChildren<Button>(true).FirstOrDefault(button => button.name.Contains("Back"));
        if (back == null)
            return;

        SetButtonText(back, "BACK");
        ClearPersistent(back.onClick);
        UnityEventTools.AddPersistentListener(back.onClick, action);
        DisableButtonIcon(back.transform);
        EditorUtility.SetDirty(back);
    }

    private static void PatchSceneScreenTitles(Transform root)
    {
        SetSceneTitle(root, "OptionsMenuText (TMP)", "SETTINGS");
        SetSceneTitle(root, "SoundMenuText (TMP)", "SOUND SETTINGS");
        SetSceneTitle(root, "GraphicsMenuText (TMP)", "GRAPHICS SETTINGS");
        SetSceneTitle(root, "KeybindsMenuText (TMP)", "KEYBINDS SETTINGS");
        SetSceneTitle(root, "StatisticsMenuText (TMP)", "PLAYER STATS");
    }

    private static void SetSceneTitle(Transform root, string objectName, string text)
    {
        TMP_Text title = FindDeepChild(root, objectName)?.GetComponent<TMP_Text>();
        if (title == null)
            return;

        title.text = text;
        title.textWrappingMode = TextWrappingModes.NoWrap;
        title.overflowMode = TextOverflowModes.Overflow;
        title.rectTransform.sizeDelta = new Vector2(520f, Mathf.Max(52f, title.rectTransform.sizeDelta.y));
        EnsureTitleFrame(title.transform.parent, title.rectTransform);
    }

    private static void PatchPlayerProfileStatsReferences()
    {
        PlayerProfileStats stats = Object.FindFirstObjectByType<PlayerProfileStats>(FindObjectsInactive.Include);
        if (stats == null)
            return;

        SerializedObject serialized = new(stats);
        SetSerializedObject(serialized, "OppsElimText", FindTmpByName("StatOppsElimValueText"));
        SetSerializedObject(serialized, "TimesElimText", FindTmpByName("StatTimesElimValueText"));
        SetSerializedObject(serialized, "PowerUpsPickedUpText", FindTmpByName("StatTotalPowValueText"));
        SetSerializedObject(serialized, "WinsText", FindTmpByName("StatWinsValueText"));
        SetSerializedObject(serialized, "LossesText", FindTmpByName("StatLossesValueText"));
        SetSerializedObject(serialized, "DistDrivenText", FindTmpByName("StatDistDrivenValueText"));
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PatchMultiplayerConnectionScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MultiplayerConnectionScenePath, OpenSceneMode.Single);
        Transform canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault()?.transform;
        if (canvas == null)
            throw new InvalidOperationException("MultiplayerConnection scene has no Canvas.");

        PatchConnectionBackground(canvas);
        PatchConnectionSceneLayout(canvas);
        RemoveDuplicateText(canvas, "MULTIPLAYER");
        RemoveDuplicateText(canvas, "ENTER HOST ADDRESS");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchConnectionBackground(Transform canvas)
    {
        Image background = EnsureChildImage(canvas, "CyberConnectionBackground");
        background.transform.SetAsFirstSibling();
        background.color = new Color(0.01f, 0.11f, 0.15f, 0.72f);
        background.raycastTarget = false;
        Stretch(background.rectTransform, Vector2.zero);

        Image grid = EnsureChildImage(canvas, "CyberConnectionGrid");
        grid.transform.SetSiblingIndex(1);
        grid.color = new Color(0f, 0.92f, 1f, 0.08f);
        grid.raycastTarget = false;
        Stretch(grid.rectTransform, Vector2.zero);
    }

    private static void PatchConnectionSceneLayout(Transform canvas)
    {
        TMP_Text title = FindTextWithExactContent(canvas, "MULTIPLAYER");
        if (title != null)
        {
            title.fontSize = 56f;
            title.textWrappingMode = TextWrappingModes.NoWrap;
            title.overflowMode = TextOverflowModes.Overflow;
            title.rectTransform.sizeDelta = new Vector2(680f, 82f);
            title.rectTransform.anchoredPosition = new Vector2(0f, -58f);
        }

        PositionConnectionCard(canvas, "HostSessionCard", new Vector2(-310f, -130f), "HOST");
        PositionConnectionCard(canvas, "JoinSessionCard", new Vector2(310f, -130f), "JOIN");

        Transform mainBack = FindDeepChild(canvas, "MainBackButton");
        if (mainBack != null)
        {
            RectTransform rect = mainBack.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(280f, 54f);
            rect.anchoredPosition = new Vector2(0f, 64f);
            SetButtonText(mainBack.GetComponentInChildren<Button>(true), "BACK");
            DisableButtonIcon(mainBack);
        }

        Transform joinPanel = FindDeepChild(canvas, "JoinPanel");
        if (joinPanel != null)
        {
            RectTransform rect = joinPanel.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(640f, 380f);
            rect.anchoredPosition = new Vector2(0f, -42f);
            TMP_Text address = FindDeepChild(joinPanel, "AddressLabel")?.GetComponent<TMP_Text>();
            if (address != null)
            {
                address.text = "ENTER HOST ADDRESS";
                address.fontSize = 24f;
                address.textWrappingMode = TextWrappingModes.NoWrap;
            }

            TMP_InputField input = joinPanel.GetComponentInChildren<TMP_InputField>(true);
            if (input != null)
            {
                RectTransform inputRect = input.GetComponent<RectTransform>();
                inputRect.sizeDelta = new Vector2(430f, 56f);
                Image image = EnsureComponent<Image>(input.gameObject);
                image.color = new Color(0.02f, 0.18f, 0.22f, 0.62f);
                Outline outline = EnsureComponent<Outline>(input.gameObject);
                outline.effectColor = new Color(0f, 0.9f, 1f, 0.38f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
        }
    }

    private static void PositionConnectionCard(Transform root, string name, Vector2 position, string buttonText)
    {
        Transform card = FindDeepChild(root, name);
        if (card == null)
            return;

        RectTransform rect = card.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(480f, 300f);
        rect.anchoredPosition = position;
        SetButtonText(card.GetComponentInChildren<Button>(true), buttonText);
    }

    private static void PatchLobbyScene()
    {
        Scene scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
        foreach (LobbyController controller in Object.FindObjectsByType<LobbyController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            PatchLobbyTrailColorReferences(controller);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchGameOverScene()
    {
        Scene scene = EditorSceneManager.OpenScene(GameOverScenePath, OpenSceneMode.Single);
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (NormalizeText(text.text) == "GAME OVER")
                text.text = "MATCH RESULTS";
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void CleanStaleRenameMetadata()
    {
        ReplaceFileText(MainMenuScenePath,
            ("Assembly-CSharp::Menu", "Assembly-CSharp::MainMenuController"));
        ReplaceFileText(GameOverScenePath,
            ("Assembly-CSharp::GameOverController", "Assembly-CSharp::MatchResultsController"),
            ("Assembly-CSharp::GameOverResultRow", "Assembly-CSharp::MatchResultRow"));
        ReplaceFileText("Assets/Project/Prefabs/UI/Leaderboard/MatchResultRow.prefab",
            ("Assembly-CSharp::GameOverResultRow", "Assembly-CSharp::MatchResultRow"));
        ReplaceFileText("Assets/Project/Prefabs/UI/Leaderboard/MatchResultsPanel.prefab",
            ("Assembly-CSharp::GameOverController", "Assembly-CSharp::MatchResultsController"),
            ("Assembly-CSharp::GameOverResultRow", "Assembly-CSharp::MatchResultRow"));
    }

    private static void VerifyNoMissingScripts(List<string> failures)
    {
        string[] paths =
        {
            MainMenuScenePath,
            MultiplayerConnectionScenePath,
            LobbyScenePath,
            GameOverScenePath,
            MenuButtonPrefabPath,
            MenuBackButtonPrefabPath,
            TrailColorButtonPrefabPath,
            SingleplayerLobbyRootPrefabPath,
            MultiplayerLobbyRootPrefabPath,
            PlayerStatsScreenPrefabPath
        };

        foreach (string path in paths)
        {
            if (path.EndsWith(".unity", StringComparison.Ordinal))
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (GameObject root in scene.GetRootGameObjects())
                    VerifyMissingScriptsRecursive(path, root, failures);
            }
            else
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                VerifyMissingScriptsRecursive(path, root, failures);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void VerifyMainMenu(List<string> failures)
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            failures.Add("MainMenu scene missing MainMenuController.");
            return;
        }

        foreach ((string buttonName, string label) in RequiredMainMenuLabels)
        {
            Button button = FindDeepChild(controller.transform.root, buttonName)?.GetComponentInChildren<Button>(true);
            if (button == null)
            {
                failures.Add($"MainMenu missing {buttonName}.");
                continue;
            }

            string actual = FirstButtonText(button.gameObject);
            if (actual != label)
                failures.Add($"{buttonName} label is '{actual}', expected '{label}'.");
        }
    }

    private static void VerifySettingsPrefabs(List<string> failures)
    {
        VerifyTitlePrefab(MainSettingsScreenPrefabPath, "SETTINGS", failures);
        VerifyTitlePrefab(SoundSettingsScreenPrefabPath, "SOUND SETTINGS", failures);
        VerifyTitlePrefab(GraphicsSettingsScreenPrefabPath, "GRAPHICS SETTINGS", failures);
        VerifyTitlePrefab(ControlsSettingsScreenPrefabPath, "KEYBINDS SETTINGS", failures);
        VerifyTitlePrefab(PlayerStatsScreenPrefabPath, "PLAYER STATS", failures);
    }

    private static void VerifyTitlePrefab(string path, string title, List<string> failures)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        TMP_Text text = root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(candidate => NormalizeText(candidate.text) == title);
        if (text == null)
            failures.Add($"{path} missing title '{title}'.");
        else if (text.rectTransform.sizeDelta.x < 420f || text.textWrappingMode != TextWrappingModes.NoWrap)
            failures.Add($"{path} title '{title}' can wrap or is too narrow.");
        if (FindDeepChild(root.transform, "TitleFrame") == null)
            failures.Add($"{path} missing TitleFrame.");
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void VerifyLobbyPrefabs(List<string> failures)
    {
        VerifyLobbyPrefab(SingleplayerLobbyRootPrefabPath, "SINGLEPLAYER", failures);
        VerifyLobbyPrefab(MultiplayerLobbyRootPrefabPath, "MULTIPLAYER", failures);
    }

    private static void VerifyLobbyPrefab(string path, string title, List<string> failures)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        TMP_Text titleText = root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault(text => NormalizeText(text.text) == title);
        if (titleText == null)
            failures.Add($"{path} missing lobby title {title}.");
        if (IsActiveHighAlphaOverlay(root, "AtmosphereBackground"))
            failures.Add($"{path} has active high-alpha AtmosphereBackground.");
        if (IsActiveHighAlphaOverlay(root, "ReadabilityScrim"))
            failures.Add($"{path} has active high-alpha ReadabilityScrim.");
        if (!root.GetComponentsInChildren<Light>(true).Any(light => light.type == LightType.Directional))
            failures.Add($"{path} missing directional light.");
        Camera camera = root.GetComponentInChildren<Camera>(true);
        if (camera != null && camera.clearFlags == CameraClearFlags.SolidColor && camera.backgroundColor.maxColorComponent < 0.2f)
            failures.Add($"{path} camera still uses dark flat solid background.");
        foreach (string label in new[] { "Match duration", "Game mode", "Sudden death", "Trail duration" })
        {
            if (!root.GetComponentsInChildren<TMP_Text>(true).Any(text => NormalizeText(text.text) == NormalizeText(label)))
                failures.Add($"{path} missing match setting label '{label}'.");
        }
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void VerifyTrailColorButton(List<string> failures)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(TrailColorButtonPrefabPath);
        Image preview = FindDeepChild(root.transform, "ColorPreview")?.GetComponent<Image>();
        Button button = root.GetComponent<Button>();
        MenuSelectable selectable = root.GetComponent<MenuSelectable>();
        SerializedObject selectableSerialized = selectable != null ? new SerializedObject(selectable) : null;
        Object targetOverride = selectableSerialized?.FindProperty("targetGraphicOverride")?.objectReferenceValue;
        if (preview == null)
            failures.Add("TrailColorButton missing ColorPreview.");
        if (button != null && preview != null && button.targetGraphic == preview)
            failures.Add("TrailColorButton Button.targetGraphic points to ColorPreview.");
        if (preview != null && targetOverride == preview)
            failures.Add("TrailColorButton MenuSelectable targetGraphicOverride points to ColorPreview.");
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void VerifyPlayerStatsPrefab(List<string> failures)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerStatsScreenPrefabPath);
        GridLayoutGroup grid = FindDeepChild(root.transform, "StatsGrid")?.GetComponent<GridLayoutGroup>();
        if (grid == null)
            failures.Add("PlayerStatsScreen missing StatsGrid.");
        else if (grid.cellSize.x <= 0f || grid.cellSize.y <= 0f)
            failures.Add("PlayerStatsScreen StatsGrid has invalid card size.");
        string[] required =
        {
            "StatOppsElimValueText",
            "StatTimesElimValueText",
            "StatTotalPowValueText",
            "StatWinsValueText",
            "StatLossesValueText",
            "StatDistDrivenValueText"
        };
        foreach (string name in required)
        {
            if (FindDeepChild(root.transform, name) == null)
                failures.Add($"PlayerStatsScreen missing {name}.");
        }
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void VerifyMultiplayerConnection(List<string> failures)
    {
        Scene scene = EditorSceneManager.OpenScene(MultiplayerConnectionScenePath, OpenSceneMode.Single);
        Transform canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault()?.transform;
        if (canvas == null)
        {
            failures.Add("MultiplayerConnection missing Canvas.");
            return;
        }

        if (CountTexts(canvas, "MULTIPLAYER") != 1)
            failures.Add("MultiplayerConnection should have exactly one MULTIPLAYER title.");
        if (FindDeepChild(canvas, "HostSessionCard") == null)
            failures.Add("MultiplayerConnection missing HostSessionCard.");
        if (FindDeepChild(canvas, "JoinSessionCard") == null)
            failures.Add("MultiplayerConnection missing JoinSessionCard.");
        if (CountTexts(canvas, "ENTER HOST ADDRESS") != 1)
            failures.Add("MultiplayerConnection should have exactly one ENTER HOST ADDRESS label.");
    }

    private static void VerifyStaleMetadata(List<string> failures)
    {
        foreach (string path in new[] { MainMenuScenePath, GameOverScenePath, "Assets/Project/Prefabs/UI/Leaderboard/MatchResultRow.prefab", "Assets/Project/Prefabs/UI/Leaderboard/MatchResultsPanel.prefab" })
        {
            string text = File.ReadAllText(path);
            if (text.Contains("Assembly-CSharp::Menu") ||
                text.Contains("GameOverController") ||
                text.Contains("GameOverResultRow"))
            {
                failures.Add($"{path} still contains stale renamed class metadata.");
            }
        }
    }

    private static bool IsActiveHighAlphaOverlay(GameObject root, string name)
    {
        Transform overlay = FindDeepChild(root.transform, name);
        if (overlay == null || !overlay.gameObject.activeSelf)
            return false;

        Image image = overlay.GetComponent<Image>();
        return image != null && image.color.a > 0.35f;
    }

    private static void VerifyMissingScriptsRecursive(string path, GameObject gameObject, List<string> failures)
    {
        int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
        if (count > 0)
            failures.Add($"{path}/{gameObject.name} has {count} missing script component(s).");

        for (int i = 0; i < gameObject.transform.childCount; i++)
            VerifyMissingScriptsRecursive(path, gameObject.transform.GetChild(i).gameObject, failures);
    }

    private static void PatchPrefab(string path, Action<GameObject> patch)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        patch(root);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void ConfigureSelectable(GameObject target, MenuSelectableVisualPreset visual, MenuSelectableAudioPreset audio, Graphic targetGraphic, bool useColorTint, MenuSelectionPersistence persistence, bool clearSelection)
    {
        Selectable selectable = EnsureComponent<Selectable>(target);
        if (targetGraphic != null)
            selectable.targetGraphic = targetGraphic;

        MenuSelectable menuSelectable = EnsureComponent<MenuSelectable>(target);
        SerializedObject serialized = new(menuSelectable);
        SetSerializedObject(serialized, "selectable", selectable);
        SetSerializedObject(serialized, "targetGraphicOverride", targetGraphic);
        SetSerializedObject(serialized, "visualPreset", visual);
        SetSerializedObject(serialized, "audioPreset", audio);
        SetSerializedBool(serialized, "useColorTint", useColorTint);
        SetEnum(serialized, "selectionPersistence", (int)persistence);
        SetSerializedBool(serialized, "clearEventSystemSelectionOnPointerUp", clearSelection);
        serialized.ApplyModifiedPropertiesWithoutUndo();

        selectable.transition = useColorTint && visual != null ? Selectable.Transition.ColorTint : Selectable.Transition.None;
        if (useColorTint && visual != null)
            selectable.colors = visual.ToColorBlock();
    }

    private static void ConfigureStateGraphics(GameObject target, Color hoverColor, Graphic selectedGlow = null)
    {
        Selectable selectable = target.GetComponent<Selectable>();
        MenuSelectableStateGraphics state = EnsureComponent<MenuSelectableStateGraphics>(target);
        Image hover = FindDeepChild(target.transform, "HoverGlow")?.GetComponent<Image>();
        if (hover != null)
        {
            hover.color = hoverColor;
            hover.raycastTarget = false;
        }

        SerializedObject serialized = new(state);
        SetSerializedObject(serialized, "selectable", selectable);
        SetSerializedObject(serialized, "hoverGlow", hover);
        if (selectedGlow != null)
            SetSerializedObject(serialized, "selectedGlow", selectedGlow);
        SetEnum(serialized, "selectionPersistence", (int)(target.GetComponent<MenuSelectable>()?.SelectionPersistence ?? MenuSelectionPersistence.None));
        SetSerializedColor(serialized, "normalTextColor", new Color(0.90f, 1f, 0.98f, 1f));
        SetSerializedColor(serialized, "disabledTextColor", new Color(0.35f, 0.46f, 0.48f, 0.72f));
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetButtonText(Button button, string label)
    {
        if (button == null)
            return;

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
            return;

        text.text = label;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = new Color(0.92f, 1f, 0.98f, 1f);
        EditorUtility.SetDirty(text);
    }

    private static string FirstButtonText(GameObject root)
    {
        return root.GetComponentsInChildren<TMP_Text>(true).FirstOrDefault()?.text;
    }

    private static void SetTextByName(GameObject root, string objectName, string value, float fontSize)
    {
        TMP_Text text = FindDeepChild(root.transform, objectName)?.GetComponent<TMP_Text>();
        if (text == null)
            return;

        text.text = value;
        text.fontSize = fontSize;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    private static TMP_Text CreateTmpChild(Transform parent, string name)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TMP_Text>();
    }

    private static TMP_Text EnsureTmpChild(Transform parent, string name)
    {
        Transform existing = FindDeepChild(parent, name);
        return existing != null ? existing.GetComponent<TMP_Text>() : CreateTmpChild(parent, name);
    }

    private static Image EnsureChildImage(Transform parent, string name)
    {
        GameObject child = EnsureChild(parent, name, typeof(RectTransform), typeof(Image));
        return child.GetComponent<Image>();
    }

    private static GameObject EnsureChild(Transform parent, string name, params Type[] components)
    {
        Transform existing = FindDeepChild(parent, name);
        if (existing != null)
        {
            foreach (Type type in components)
            {
                if (existing.GetComponent(type) == null)
                    existing.gameObject.AddComponent(type);
            }

            return existing.gameObject;
        }

        GameObject child = new(name, components);
        child.transform.SetParent(parent, false);
        return child;
    }

    private static T EnsureComponent<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        if (component == null)
            component = gameObject.AddComponent<T>();
        return component;
    }

    private static void Stretch(RectTransform rect, Vector2 inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = inset;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;

        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform result = FindDeepChild(root.GetChild(i), name);
            if (result != null)
                return result;
        }

        return null;
    }

    private static TMP_Text FindTmpByName(string name)
    {
        return Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(text => text.name == name);
    }

    private static TMP_Text FindTextWithExactContent(Transform root, string content)
    {
        return root.GetComponentsInChildren<TMP_Text>(true)
            .FirstOrDefault(text => NormalizeText(text.text) == NormalizeText(content));
    }

    private static int CountTexts(Transform root, string content)
    {
        return root.GetComponentsInChildren<TMP_Text>(true)
            .Count(text => NormalizeText(text.text) == NormalizeText(content));
    }

    private static void RemoveDuplicateText(Transform root, string content)
    {
        TMP_Text[] matches = root.GetComponentsInChildren<TMP_Text>(true)
            .Where(text => NormalizeText(text.text) == NormalizeText(content))
            .ToArray();

        for (int i = 1; i < matches.Length; i++)
            Object.DestroyImmediate(matches[i].gameObject);
    }

    private static string NormalizeText(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace("\n", " ").Replace("\r", " ").Trim().ToUpperInvariant();
    }

    private static void DisableButtonIcon(Transform root, string buttonName)
    {
        Transform button = FindDeepChild(root, buttonName);
        if (button != null)
            DisableButtonIcon(button);
    }

    private static void DisableButtonIcon(Transform button)
    {
        SetChildActive(button, "ButtonIcon", false);
        SetChildActive(button, "Back Icon", false);
    }

    private static void SetChildActive(Transform root, string childName, bool active)
    {
        Transform child = FindDeepChild(root, childName);
        if (child != null)
            child.gameObject.SetActive(active);
    }

    private static void ClearPersistent(UnityEvent unityEvent)
    {
        for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(unityEvent, i);
    }

    private static void SetSerializedObject(SerializedObject serialized, string name, Object value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetSerializedBool(SerializedObject serialized, string name, bool value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
            property.boolValue = value;
    }

    private static void SetSerializedColor(SerializedObject serialized, string name, Color value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
            property.colorValue = value;
    }

    private static void SetEnum(SerializedObject serialized, string name, int value)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property != null)
            property.enumValueIndex = value;
    }

    private static T LoadAsset<T>(string path, bool required = true) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null && required)
            throw new InvalidOperationException($"Missing asset at {path}.");
        return asset;
    }

    private static void EnsureFolder(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folder = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void ReplaceFileText(string path, params (string From, string To)[] replacements)
    {
        string text = File.ReadAllText(path);
        string updated = text;
        foreach ((string from, string to) in replacements)
            updated = updated.Replace(from, to);

        if (updated != text)
            File.WriteAllText(path, updated);
    }
}
