using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class Milestone8UiOverhaulPatcher
{
    private const string MainMenuScenePath = "Assets/Project/Scenes/UI/Menu/MainMenu.unity";
    private const string MultiplayerConnectionScenePath = "Assets/Project/Scenes/UI/Menu/MultiplayerConnection.unity";
    private const string LobbyScenePath = "Assets/Project/Scenes/UI/Lobby/LobbyScene.unity";
    private const string GameOverScenePath = "Assets/Project/Scenes/UI/Menu/GameOver.unity";

    private const string SingleplayerLobbyRootPrefabPath = "Assets/Project/Prefabs/UI/Lobby/SingleplayerLobbyRoot.prefab";
    private const string MultiplayerLobbyRootPrefabPath = "Assets/Project/Prefabs/UI/Lobby/MultiplayerLobbyRoot.prefab";
    private const string MatchResultRowPrefabPath = "Assets/Project/Prefabs/UI/Leaderboard/MatchResultRow.prefab";

    private const string DefaultPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Default.asset";
    private const string DarkerPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Darker.asset";
    private const string DangerPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Danger.asset";
    private const string DisabledSlotPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_DisabledSlot.asset";
    private const string BackPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Back.asset";
    private const string AddBotPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_AddBot.asset";
    private const string DefaultAudioPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuAudio_Default.asset";

    private const string RajdhaniFontAssetPath = "Assets/Project/Art/UI/TextMesh Pro/Resources/Fonts & Materials/Rajdhani SDF.asset";
    private const string OrbitronFontAssetPath = "Assets/Project/Art/UI/TextMesh Pro/Resources/Fonts & Materials/Orbitron SDF.asset";
    private const string RajdhaniSourcePath = "Assets/Project/Art/UI/TextMesh Pro/Fonts/Rajdhani-SemiBold.ttf";
    private const string OrbitronSourcePath = "Assets/Project/Art/UI/TextMesh Pro/Fonts/Orbitron-Bold.ttf";

    private static readonly string[] CoreSelectablePrefabPaths =
    {
        "Assets/Project/Prefabs/UI/Menu/MenuButton.prefab",
        "Assets/Project/Prefabs/UI/Menu/MenuBackButton.prefab",
        "Assets/Project/Prefabs/UI/Menu/MenuSelectorButton.prefab",
        "Assets/Project/Prefabs/UI/Menu/MenuToggle.prefab",
        "Assets/Project/Prefabs/UI/Menu/MenuSlider.prefab",
        "Assets/Project/Prefabs/UI/Menu/MenuDropdown.prefab",
        "Assets/Project/Prefabs/UI/Lobby/TrailColorButton.prefab",
        "Assets/Project/Prefabs/UI/Lobby/OpponentSlot.prefab",
        "Assets/Project/Prefabs/UI/Lobby/Panels/MatchSettingsPanel.prefab",
        "Assets/Project/Prefabs/UI/Lobby/Panels/OpponentSlotsPanel.prefab",
        "Assets/Project/Prefabs/UI/Lobby/Panels/ScooterSelectionPanel.prefab",
        "Assets/Project/Prefabs/UI/Lobby/Panels/TrailColorSelectionPanel.prefab"
    };

    private static readonly Dictionary<string, string> MainMenuScreenRenames = new()
    {
        { "OptionsScreen", "MainMenuSettingsScreen" },
        { "SoundScreen", "SoundSettingsScreen" },
        { "GraphicsScreen", "GraphicsSettingsScreen" },
        { "KeybindsScreen", "ControlsSettingsScreen" },
        { "StatisticsScreen", "PlayerStatsScreen" }
    };

    [MenuItem("Throne/Tools/Migrations/DO NOT RUN - Legacy Migration/Run Milestone 8 UI Overhaul")]
    public static void Run()
    {
        if (!ConfirmLegacyRun("Milestone 8 UI Overhaul"))
            return;

        TMP_FontAsset rajdhani = EnsureFontAsset(RajdhaniSourcePath, RajdhaniFontAssetPath);
        TMP_FontAsset orbitron = EnsureFontAsset(OrbitronSourcePath, OrbitronFontAssetPath);

        PatchVisualPresets();
        PatchCorePrefabs(rajdhani, orbitron);
        PatchLobbyRootPrefab(SingleplayerLobbyRootPrefabPath, false, rajdhani, orbitron);
        PatchLobbyRootPrefab(MultiplayerLobbyRootPrefabPath, true, rajdhani, orbitron);
        PatchLobbyScene();
        PatchMainMenuScene(rajdhani, orbitron);
        PatchMultiplayerConnectionScene(rajdhani, orbitron);
        PatchMatchResultsScene(rajdhani, orbitron);
        PatchMatchResultRowPrefab(rajdhani);
        PatchUiPrefabPersistentTargetTypes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Milestone8UiOverhaulPatcher] Patch complete.");
    }

    private static bool ConfirmLegacyRun(string migrationName)
    {
        const string message = "This is a legacy one-shot global UI migration. It can reapply broad colors, layout changes, and target-graphic policies. Use the Milestone 8.2 targeted repair/verifier instead unless you are intentionally replaying history.";
        if (Application.isBatchMode)
        {
            Debug.LogError($"[{nameof(Milestone8UiOverhaulPatcher)}] Blocked legacy migration '{migrationName}' in batch mode. {message}");
            return false;
        }

        return EditorUtility.DisplayDialog(
            $"Legacy migration: {migrationName}",
            message,
            "Run Legacy Migration",
            "Cancel");
    }

    [MenuItem("Throne/Tools/Migrations/Verify Milestone 8 UI Overhaul")]
    public static void Verify()
    {
        VerifyNoMissingScripts();
        VerifyRenamedControllers();
        VerifyLobbyRoots();
        VerifyCoreSelectables();
        VerifyGameOverScene();
        Debug.Log("[Milestone8UiOverhaulPatcher] Verification complete.");
    }

    public static void RunAndVerify()
    {
        Run();
        Verify();
    }

    private static TMP_FontAsset EnsureFontAsset(string sourceFontPath, string targetAssetPath)
    {
        TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetAssetPath);
        if (HasValidFontAtlas(existing))
            return existing;

        Font sourceFont = LoadAsset<Font>(sourceFontPath);
        if (existing != null)
            AssetDatabase.DeleteAsset(targetAssetPath);

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        if (fontAsset == null)
            throw new InvalidOperationException($"Failed to create TMP font asset from {sourceFontPath}.");

        EnsureFolder(Path.GetDirectoryName(targetAssetPath)?.Replace('\\', '/'));
        fontAsset.name = Path.GetFileNameWithoutExtension(targetAssetPath);
        AssetDatabase.CreateAsset(fontAsset, targetAssetPath);
        PersistFontAssetResources(fontAsset);
        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    private static bool HasValidFontAtlas(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null || fontAsset.material == null)
            return false;

        string fontAssetPath = AssetDatabase.GetAssetPath(fontAsset);
        Texture2D atlasTexture = fontAsset.atlasTexture;
        return atlasTexture != null
            && AssetDatabase.GetAssetPath(atlasTexture) == fontAssetPath
            && AssetDatabase.GetAssetPath(fontAsset.material) == fontAssetPath
            && fontAsset.material.GetTexture(ShaderUtilities.ID_MainTex) == atlasTexture;
    }

    private static void PersistFontAssetResources(TMP_FontAsset fontAsset)
    {
        Texture2D atlasTexture = fontAsset.atlasTexture;
        if (atlasTexture != null)
        {
            atlasTexture.name = $"{fontAsset.name} Atlas";
            if (AssetDatabase.GetAssetPath(atlasTexture) != AssetDatabase.GetAssetPath(fontAsset))
                AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
        }

        Material material = fontAsset.material;
        if (material == null)
            return;

        material.name = $"{fontAsset.name} Material";
        if (atlasTexture != null)
            material.SetTexture(ShaderUtilities.ID_MainTex, atlasTexture);

        if (AssetDatabase.GetAssetPath(material) != AssetDatabase.GetAssetPath(fontAsset))
            AssetDatabase.AddObjectToAsset(material, fontAsset);
    }

    private static void PatchVisualPresets()
    {
        ConfigurePreset(DefaultPresetPath,
            new Color(0.025f, 0.2f, 0.24f, 0.94f),
            new Color(0.16f, 0.92f, 1f, 1f),
            new Color(0.02f, 0.55f, 0.68f, 1f),
            new Color(0.12f, 0.74f, 0.95f, 1f),
            new Color(0.035f, 0.06f, 0.07f, 0.52f));

        ConfigurePreset(DarkerPresetPath,
            new Color(0.02f, 0.055f, 0.07f, 0.92f),
            new Color(0.11f, 0.32f, 0.38f, 1f),
            new Color(0.04f, 0.18f, 0.22f, 1f),
            new Color(0.09f, 0.26f, 0.32f, 1f),
            new Color(0.025f, 0.035f, 0.04f, 0.5f));

        ConfigurePreset(DangerPresetPath,
            new Color(0.24f, 0.04f, 0.08f, 0.92f),
            new Color(0.88f, 0.18f, 0.28f, 1f),
            new Color(0.62f, 0.08f, 0.14f, 1f),
            new Color(0.7f, 0.12f, 0.18f, 1f),
            new Color(0.08f, 0.025f, 0.03f, 0.5f));

        ConfigurePreset(DisabledSlotPresetPath,
            new Color(0.02f, 0.04f, 0.045f, 0.62f),
            new Color(0.06f, 0.11f, 0.12f, 0.72f),
            new Color(0.03f, 0.07f, 0.08f, 0.72f),
            new Color(0.06f, 0.11f, 0.12f, 0.72f),
            new Color(0.012f, 0.018f, 0.02f, 0.48f));

        ConfigurePreset(BackPresetPath,
            new Color(0.015f, 0.045f, 0.055f, 0.9f),
            new Color(0.11f, 0.24f, 0.28f, 1f),
            new Color(0.04f, 0.11f, 0.13f, 1f),
            new Color(0.08f, 0.18f, 0.21f, 1f),
            new Color(0.015f, 0.02f, 0.025f, 0.5f));

        ConfigurePreset(AddBotPresetPath,
            new Color(0.02f, 0.18f, 0.145f, 0.9f),
            new Color(0.16f, 0.72f, 0.56f, 1f),
            new Color(0.04f, 0.38f, 0.31f, 1f),
            new Color(0.1f, 0.55f, 0.44f, 1f),
            new Color(0.018f, 0.045f, 0.04f, 0.48f));
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

    private static void PatchCorePrefabs(TMP_FontAsset rajdhani, TMP_FontAsset orbitron)
    {
        foreach (string path in CoreSelectablePrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ApplyFonts(root, rajdhani, orbitron);
                NormalizeSelectables(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void PatchLobbyRootPrefab(string prefabPath, bool multiplayer, TMP_FontAsset rajdhani, TMP_FontAsset orbitron)
    {
        GameObject root = LoadAsset<GameObject>(prefabPath);
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            contents.name = multiplayer ? "MultiplayerLobbyRoot" : "SingleplayerLobbyRoot";
            SetText(contents, "LobbyText (TMP)", multiplayer ? "MULTIPLAYER" : "SINGLEPLAYER");
            SetText(contents, "PlayersText (TMP)", multiplayer ? "PLAYERS" : "BOTS");
            PatchLobbyLighting(contents, multiplayer);
            PatchScooterPreviewSettings(contents);
            PatchTrailColorAlphas(contents);
            ApplyFonts(contents, rajdhani, orbitron);
            NormalizeSelectables(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void PatchLobbyLighting(GameObject root, bool multiplayer)
    {
        Light keyLight = EnsureLight(root.transform, "MoodKeyLight", LightType.Directional);
        Light rimLight = EnsureLight(root.transform, "NeonRimLight", LightType.Point);
        Light fillLight = EnsureLight(root.transform, "ShowroomFillLight", LightType.Point);

        if (multiplayer)
        {
            keyLight.color = new Color(0.36f, 0.44f, 1f, 1f);
            keyLight.intensity = 0.52f;
            keyLight.transform.localRotation = Quaternion.Euler(24f, 152f, 0f);
            rimLight.color = new Color(0.62f, 0.2f, 1f, 1f);
            rimLight.intensity = 3.8f;
            fillLight.color = new Color(0f, 0.85f, 1f, 1f);
            fillLight.intensity = 1.5f;
        }
        else
        {
            keyLight.color = new Color(0.5f, 0.92f, 1f, 1f);
            keyLight.intensity = 0.62f;
            keyLight.transform.localRotation = Quaternion.Euler(34f, -28f, 0f);
            rimLight.color = new Color(1f, 0.42f, 0.72f, 1f);
            rimLight.intensity = 2.3f;
            fillLight.color = new Color(0f, 0.82f, 1f, 1f);
            fillLight.intensity = 1.9f;
        }

        rimLight.range = 12f;
        rimLight.transform.localPosition = new Vector3(3.5f, 2.4f, -2.2f);
        fillLight.range = 10f;
        fillLight.transform.localPosition = new Vector3(-3.8f, 2.2f, 2.5f);

        foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = multiplayer
                ? new Color(0.006f, 0.008f, 0.028f, 1f)
                : new Color(0.01f, 0.018f, 0.026f, 1f);
        }
    }

    private static Light EnsureLight(Transform root, string name, LightType type)
    {
        Transform existing = FindDeepChild(root, name);
        GameObject lightObject = existing != null ? existing.gameObject : new GameObject(name);
        lightObject.transform.SetParent(root, false);
        Light light = lightObject.GetComponent<Light>();
        if (light == null)
            light = lightObject.AddComponent<Light>();

        light.type = type;
        return light;
    }

    private static void PatchScooterPreviewSettings(GameObject root)
    {
        LobbyController lobby = root.GetComponentInChildren<LobbyController>(true);
        if (lobby == null)
            return;

        SerializedObject serialized = new(lobby);
        SerializedProperty scooters = serialized.FindProperty("scooters");
        SerializedProperty motorPreview = scooters.FindPropertyRelative("motorPreview");
        SerializedProperty previewEntries = scooters.FindPropertyRelative("previewEntries");
        int count = motorPreview != null ? motorPreview.arraySize : 0;
        previewEntries.arraySize = count;

        for (int i = 0; i < count; i++)
        {
            SerializedProperty entry = previewEntries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("previewPrefab").objectReferenceValue = motorPreview.GetArrayElementAtIndex(i).objectReferenceValue;
            entry.FindPropertyRelative("localPosition").vector3Value = i == 0
                ? new Vector3(0f, 0.28f, 0f)
                : new Vector3(0f, 0.18f, 0f);
            entry.FindPropertyRelative("localEulerAngles").vector3Value = new Vector3(0f, 146f, 0f);
            entry.FindPropertyRelative("localScale").vector3Value = i == 0
                ? new Vector3(1.45f, 1.45f, 1.45f)
                : new Vector3(1.55f, 1.55f, 1.55f);
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(lobby);
    }

    private static void PatchTrailColorAlphas(GameObject root)
    {
        foreach (LobbyController lobby in root.GetComponentsInChildren<LobbyController>(true))
        {
            SerializedObject serialized = new(lobby);
            SerializedProperty buttons = serialized.FindProperty("trailColorButtons");
            for (int i = 0; i < buttons.arraySize; i++)
            {
                SerializedProperty button = buttons.GetArrayElementAtIndex(i);
                button.FindPropertyRelative("availableAlpha").floatValue = 0.75f;
                button.FindPropertyRelative("selectedAlpha").floatValue = 0.95f;
                button.FindPropertyRelative("unavailableAlpha").floatValue = 0.35f;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(lobby);
        }
    }

    private static void PatchLobbyScene()
    {
        Scene scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
        LobbyRootSelector selector = Object.FindFirstObjectByType<LobbyRootSelector>(FindObjectsInactive.Include);
        if (selector == null)
            throw new InvalidOperationException("LobbyScene is missing LobbyRootSelector.");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchMainMenuScene(TMP_FontAsset rajdhani, TMP_FontAsset orbitron)
    {
        Scene scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (controller == null)
            throw new InvalidOperationException("MainMenu scene is missing MainMenuController.");

        foreach ((string oldName, string newName) in MainMenuScreenRenames)
            RenameObject(oldName, newName);

        SetTextByName("OptionsMenuText (TMP)", "SETTINGS");
        SetTextByName("SoundMenuText (TMP)", "SOUND SETTINGS");
        SetTextByName("GraphicsMenuText (TMP)", "GRAPHICS SETTINGS");
        SetTextByName("KeybindsMenuText (TMP)", "KEYBINDS SETTINGS");
        SetTextByName("StatisticsMenuText (TMP)", "PLAYER STATS");
        ReplaceExactText("OPTIONS", "SETTINGS");
        ReplaceExactText("Options", "SETTINGS");
        ReplaceExactText("Statistics", "PLAYER STATS");

        PatchPersistentTargetTypes("Menu, Assembly-CSharp", "MainMenuController, Assembly-CSharp");
        ApplyFontsToScene(rajdhani, orbitron);
        NormalizeSceneSelectables();
        ExtractMainMenuPrefabs();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ExtractMainMenuPrefabs()
    {
        SaveSceneObjectAsConnectedPrefab("MasterVolumeControl", "Assets/Project/Prefabs/UI/Menu/Rows/VolumeSliderRow.prefab");
        SaveSceneObjectAsConnectedPrefab("ResolutionDropdown", "Assets/Project/Prefabs/UI/Menu/Rows/GraphicsDropdownRow.prefab");
        SaveSceneObjectAsConnectedPrefab("FullscreenToggle", "Assets/Project/Prefabs/UI/Menu/Rows/FullscreenToggleRow.prefab");
        SaveSceneObjectAsConnectedPrefab("Keybind_TurnLeft", "Assets/Project/Prefabs/UI/Menu/Rows/KeybindRow.prefab");
        SaveSceneObjectAsConnectedPrefab("StatsRowLeft", "Assets/Project/Prefabs/UI/Menu/Rows/PlayerStatRow.prefab");
        SaveSceneObjectAsConnectedPrefab("MainScreen", "Assets/Project/Prefabs/UI/Menu/Screens/MainScreen.prefab");
        SaveSceneObjectAsConnectedPrefab("MainMenuSettingsScreen", "Assets/Project/Prefabs/UI/Menu/Screens/MainMenuSettingsScreen.prefab");
        SaveSceneObjectAsConnectedPrefab("SoundSettingsScreen", "Assets/Project/Prefabs/UI/Menu/Screens/SoundSettingsScreen.prefab");
        SaveSceneObjectAsConnectedPrefab("GraphicsSettingsScreen", "Assets/Project/Prefabs/UI/Menu/Screens/GraphicsSettingsScreen.prefab");
        SaveSceneObjectAsConnectedPrefab("ControlsSettingsScreen", "Assets/Project/Prefabs/UI/Menu/Screens/ControlsSettingsScreen.prefab");
        SaveSceneObjectAsConnectedPrefab("PlayerStatsScreen", "Assets/Project/Prefabs/UI/Menu/Screens/PlayerStatsScreen.prefab");
    }

    private static void PatchMultiplayerConnectionScene(TMP_FontAsset rajdhani, TMP_FontAsset orbitron)
    {
        Scene scene = EditorSceneManager.OpenScene(MultiplayerConnectionScenePath, OpenSceneMode.Single);
        MultiplayerConnectionMenu menu = Object.FindFirstObjectByType<MultiplayerConnectionMenu>(FindObjectsInactive.Include);
        if (menu == null)
            throw new InvalidOperationException("MultiplayerConnection scene is missing MultiplayerConnectionMenu.");

        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
            throw new InvalidOperationException("MultiplayerConnection scene is missing Canvas.");

        PatchConnectionBackground(canvas.transform);
        PatchConnectionCards();
        PatchConnectionBackButton();
        PatchJoinPanel();
        PatchConnectionPopup();
        ApplyFontsToScene(rajdhani, orbitron);
        NormalizeSceneSelectables();

        SaveSceneObjectAsConnectedPrefab("HostSessionCard", "Assets/Project/Prefabs/UI/Menu/Multiplayer/HostSessionCard.prefab");
        SaveSceneObjectAsConnectedPrefab("JoinSessionCard", "Assets/Project/Prefabs/UI/Menu/Multiplayer/JoinSessionCard.prefab");
        SaveSceneObjectAsConnectedPrefab("JoinPopup", "Assets/Project/Prefabs/UI/Menu/Multiplayer/ConnectionPopup.prefab");
        SaveSceneObjectAsConnectedPrefab("JoinPanel", "Assets/Project/Prefabs/UI/Menu/Multiplayer/AddressInputPanel.prefab");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void PatchConnectionBackground(Transform canvas)
    {
        Image background = EnsureImage(canvas, "CyberConnectionBackground");
        StretchToParent(background.rectTransform);
        background.transform.SetAsFirstSibling();
        background.color = new Color(0.004f, 0.012f, 0.02f, 1f);
        background.raycastTarget = false;

        Image grid = EnsureImage(canvas, "CyberConnectionGrid");
        StretchToParent(grid.rectTransform);
        grid.transform.SetSiblingIndex(1);
        grid.color = new Color(0f, 0.85f, 1f, 0.055f);
        grid.raycastTarget = false;
    }

    private static void PatchConnectionCards()
    {
        RectTransform hostCard = FindRect("HostSessionCard");
        RectTransform joinCard = FindRect("JoinSessionCard");
        ConfigureCard(hostCard, new Vector2(-310f, -35f), "HOST SESSION", "Create a local network arena and wait for players.");
        ConfigureCard(joinCard, new Vector2(310f, -35f), "JOIN SESSION", "Connect to an existing host.");
    }

    private static void ConfigureCard(RectTransform card, Vector2 position, string title, string body)
    {
        if (card == null)
            return;

        card.anchorMin = new Vector2(0.5f, 0.5f);
        card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);
        card.anchoredPosition = position;
        card.sizeDelta = new Vector2(520f, 340f);
        if (card.TryGetComponent(out Image image))
            image.color = new Color(0.012f, 0.044f, 0.058f, 0.94f);

        SetText(card.gameObject, "Title", title);
        SetText(card.gameObject, "Body", body);
    }

    private static void PatchConnectionBackButton()
    {
        Button backButton = FindButtonByName("MainBackButton");
        if (backButton == null)
            return;

        RectTransform rect = backButton.transform as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 34f);
        rect.sizeDelta = new Vector2(220f, 64f);
        SetButtonLabel(backButton.gameObject, "BACK");
    }

    private static void PatchJoinPanel()
    {
        RectTransform panel = FindRect("JoinPanel");
        if (panel == null)
            return;

        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(620f, 360f);

        TMP_Text label = FindTextByName(panel, "AddressLabel");
        if (label == null)
            label = CreateText(panel, "AddressLabel");
        label.text = "ENTER HOST ADDRESS";
        label.fontSize = 24f;
        label.alignment = TextAlignmentOptions.Center;
        RectTransform labelRect = label.transform as RectTransform;
        labelRect.anchoredPosition = new Vector2(0f, 84f);
        labelRect.sizeDelta = new Vector2(520f, 42f);

        RectTransform address = FindRect("AddressInput");
        if (address != null)
        {
            address.SetParent(panel, false);
            address.anchorMin = new Vector2(0.5f, 0.5f);
            address.anchorMax = new Vector2(0.5f, 0.5f);
            address.anchoredPosition = new Vector2(0f, 26f);
            address.sizeDelta = new Vector2(470f, 62f);
        }
    }

    private static void PatchConnectionPopup()
    {
        RectTransform popup = FindRect("JoinPopup");
        if (popup == null)
            return;

        popup.anchorMin = new Vector2(0.5f, 0.5f);
        popup.anchorMax = new Vector2(0.5f, 0.5f);
        popup.pivot = new Vector2(0.5f, 0.5f);
        popup.anchoredPosition = Vector2.zero;
        popup.sizeDelta = new Vector2(520f, 280f);
        if (popup.TryGetComponent(out Image image))
            image.color = new Color(0.01f, 0.025f, 0.032f, 0.98f);

        TMP_Text popupText = FindTextByName(popup, "PopupText");
        if (popupText != null)
        {
            popupText.text = "CONNECTION FAILED\nNo game found at given address.";
            popupText.fontSize = 26f;
            popupText.alignment = TextAlignmentOptions.Center;
        }
    }

    private static void PatchMatchResultsScene(TMP_FontAsset rajdhani, TMP_FontAsset orbitron)
    {
        Scene scene = EditorSceneManager.OpenScene(GameOverScenePath, OpenSceneMode.Single);
        MatchResultsController controller = Object.FindFirstObjectByType<MatchResultsController>(FindObjectsInactive.Include);
        if (controller == null)
            throw new InvalidOperationException("GameOver scene is missing MatchResultsController.");

        SetTextByName("TitleText (TMP)", "MATCH RESULTS");
        PatchPersistentTargetTypes("GameOverController, Assembly-CSharp", "MatchResultsController, Assembly-CSharp");
        ConfigureResultsPanel();
        ApplyFontsToScene(rajdhani, orbitron);
        NormalizeSceneSelectables();
        SaveSceneObjectAsConnectedPrefab("GameOverPanel", "Assets/Project/Prefabs/UI/Leaderboard/MatchResultsPanel.prefab");

        Button main = FindButtonByName("ReturnToMainMenuButton");
        if (main != null)
        {
            SetButtonLabel(main.gameObject, "RETURN TO MAIN MENU");
            ResizeResultButton(main, new Vector2(-190f, -260f));
            ReplacePersistentClick(main, controller.ReturnToMainMenu);
        }

        Button lobby = FindButtonByName("ReturnToLobbyButton");
        if (lobby != null)
        {
            SetButtonLabel(lobby.gameObject, "RETURN TO LOBBY");
            ResizeResultButton(lobby, new Vector2(190f, -260f));
            ReplacePersistentClick(lobby, controller.ReturnToLobby);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureResultsPanel()
    {
        RectTransform resultsRoot = FindRect("ResultsRoot");
        if (resultsRoot != null)
        {
            resultsRoot.anchorMin = new Vector2(0.5f, 0.5f);
            resultsRoot.anchorMax = new Vector2(0.5f, 0.5f);
            resultsRoot.pivot = new Vector2(0.5f, 0.5f);
            resultsRoot.anchoredPosition = new Vector2(0f, 80f);
            resultsRoot.sizeDelta = new Vector2(720f, 360f);
        }
    }

    private static void ResizeResultButton(Button button, Vector2 position)
    {
        RectTransform rect = button.transform as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(340f, 70f);
    }

    private static void PatchMatchResultRowPrefab(TMP_FontAsset rajdhani)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(MatchResultRowPrefabPath);
        try
        {
            root.name = "MatchResultRow";
            RectTransform rect = root.transform as RectTransform;
            if (rect != null)
                rect.sizeDelta = new Vector2(720f, 58f);

            ApplyFonts(root, rajdhani, null);
            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                text.fontSize = Mathf.Max(text.fontSize, 24f);

            MatchResultRow row = root.GetComponent<MatchResultRow>();
            if (row != null)
                EditorUtility.SetDirty(row);

            PrefabUtility.SaveAsPrefabAsset(root, MatchResultRowPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void NormalizeSceneSelectables()
    {
        foreach (Selectable selectable in Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            NormalizeSelectable(selectable);
    }

    private static void NormalizeSelectables(GameObject root)
    {
        foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
            NormalizeSelectable(selectable);
    }

    private static void NormalizeSelectable(Selectable selectable)
    {
        if (selectable == null)
            return;

        ColorBlock colors = selectable.colors;
        colors.colorMultiplier = 1f;
        selectable.colors = colors;

        MenuSelectable menuSelectable = selectable.GetComponent<MenuSelectable>();
        if (menuSelectable == null)
            menuSelectable = selectable.gameObject.AddComponent<MenuSelectable>();

        MenuSelectableVisualPreset preset = ResolvePresetFor(selectable);
        MenuSelectableAudioPreset audio = AssetDatabase.LoadAssetAtPath<MenuSelectableAudioPreset>(DefaultAudioPresetPath);
        Graphic target = ResolveTintTarget(selectable);
        if (target != null && IsTintTargetTooDark(target))
            target.color = new Color(1f, 1f, 1f, target.color.a);

        SerializedObject serialized = new(menuSelectable);
        serialized.FindProperty("selectable").objectReferenceValue = selectable;
        serialized.FindProperty("targetGraphicOverride").objectReferenceValue = target;
        serialized.FindProperty("visualPreset").objectReferenceValue = preset;
        serialized.FindProperty("audioPreset").objectReferenceValue = audio;
        serialized.FindProperty("useColorTint").boolValue = !selectable.name.Contains("Back");
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(menuSelectable);

        AddStateGraphics(selectable);
    }

    private static MenuSelectableVisualPreset ResolvePresetFor(Selectable selectable)
    {
        string name = selectable.name.ToLowerInvariant();
        if (name.Contains("back"))
            return LoadAsset<MenuSelectableVisualPreset>(BackPresetPath);
        if (name.Contains("add"))
            return LoadAsset<MenuSelectableVisualPreset>(AddBotPresetPath);
        if (name.Contains("remove"))
            return LoadAsset<MenuSelectableVisualPreset>(DarkerPresetPath);
        if (!selectable.interactable)
            return LoadAsset<MenuSelectableVisualPreset>(DisabledSlotPresetPath);
        return LoadAsset<MenuSelectableVisualPreset>(DefaultPresetPath);
    }

    private static void AddStateGraphics(Selectable selectable)
    {
        MenuSelectableStateGraphics state = selectable.GetComponent<MenuSelectableStateGraphics>();
        if (state == null)
            state = selectable.gameObject.AddComponent<MenuSelectableStateGraphics>();

        Image hoverGlow = EnsureStateImage(selectable.transform, "HoverGlow", new Color(0f, 0.95f, 1f, 0.13f));
        Image disabledOverlay = EnsureStateImage(selectable.transform, "DisabledOverlay", new Color(0f, 0f, 0f, 0.42f));
        hoverGlow.gameObject.SetActive(false);
        disabledOverlay.gameObject.SetActive(false);

        TMP_Text[] textTargets = selectable.GetComponentsInChildren<TMP_Text>(true);
        SerializedObject serialized = new(state);
        serialized.FindProperty("selectable").objectReferenceValue = selectable;
        serialized.FindProperty("hoverGlow").objectReferenceValue = hoverGlow;
        serialized.FindProperty("disabledOverlay").objectReferenceValue = disabledOverlay;
        SerializedProperty texts = serialized.FindProperty("textTargets");
        texts.arraySize = textTargets.Length;
        for (int i = 0; i < textTargets.Length; i++)
            texts.GetArrayElementAtIndex(i).objectReferenceValue = textTargets[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(state);
    }

    private static Image EnsureStateImage(Transform parent, string name, Color color)
    {
        Transform existing = FindDirectChild(parent, name);
        GameObject imageObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        imageObject.transform.SetAsFirstSibling();
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        StretchToParent(image.rectTransform);
        return image;
    }

    private static Graphic ResolveTintTarget(Selectable selectable)
    {
        if (IsValidTintTarget(selectable.targetGraphic))
            return selectable.targetGraphic;

        foreach (Image image in selectable.GetComponentsInChildren<Image>(true))
        {
            if (IsValidTintTarget(image))
                return image;
        }

        return selectable.targetGraphic;
    }

    private static bool IsValidTintTarget(Graphic graphic)
    {
        if (graphic == null || graphic is TMP_Text)
            return false;

        string lower = graphic.name.ToLowerInvariant();
        return !lower.Contains("icon") &&
               !lower.Contains("checkmark") &&
               !lower.Contains("text") &&
               !lower.Contains("label") &&
               !lower.Contains("glow") &&
               !lower.Contains("overlay");
    }

    private static bool IsTintTargetTooDark(Graphic graphic)
    {
        Color color = graphic.color;
        return color.r < 0.08f && color.g < 0.08f && color.b < 0.08f;
    }

    private static void ApplyFontsToScene(TMP_FontAsset rajdhani, TMP_FontAsset orbitron)
    {
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            ApplyFont(text, rajdhani, orbitron);
    }

    private static void ApplyFonts(GameObject root, TMP_FontAsset rajdhani, TMP_FontAsset orbitron)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            ApplyFont(text, rajdhani, orbitron);
    }

    private static void ApplyFont(TMP_Text text, TMP_FontAsset rajdhani, TMP_FontAsset orbitron)
    {
        if (text == null)
            return;

        text.font = IsLargeTitle(text) && orbitron != null ? orbitron : rajdhani;
        if (text.fontSize < 18f)
            text.fontSize = 18f;
        EditorUtility.SetDirty(text);
    }

    private static bool IsLargeTitle(TMP_Text text)
    {
        string value = text.text != null ? text.text.Trim() : string.Empty;
        return text.fontSize >= 34f &&
               (value == "THRONE" ||
                value == "SINGLEPLAYER" ||
                value == "MULTIPLAYER" ||
                value == "MATCH RESULTS" ||
                value == "SETTINGS" ||
                text.name.Contains("Title"));
    }

    private static void SaveSceneObjectAsConnectedPrefab(string objectName, string prefabPath)
    {
        Transform transform = FindTransformByName(objectName);
        if (transform == null)
            return;

        EnsureFolder(Path.GetDirectoryName(prefabPath)?.Replace('\\', '/'));

        if (PrefabUtility.IsPartOfPrefabInstance(transform.gameObject) &&
            PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject) != transform.gameObject)
        {
            GameObject clone = Object.Instantiate(transform.gameObject);
            clone.name = transform.gameObject.name;
            try
            {
                PrefabUtility.SaveAsPrefabAsset(clone, prefabPath);
            }
            finally
            {
                Object.DestroyImmediate(clone);
            }

            return;
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(transform.gameObject, prefabPath, InteractionMode.AutomatedAction);
    }

    private static void PatchPersistentTargetTypes(string oldTypeName, string newTypeName)
    {
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            PatchPersistentTargetType(button, "m_OnClick.m_PersistentCalls.m_Calls", oldTypeName, newTypeName);
        foreach (Toggle toggle in Object.FindObjectsByType<Toggle>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            PatchPersistentTargetType(toggle, "onValueChanged.m_PersistentCalls.m_Calls", oldTypeName, newTypeName);
        foreach (Slider slider in Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            PatchPersistentTargetType(slider, "m_OnValueChanged.m_PersistentCalls.m_Calls", oldTypeName, newTypeName);
        foreach (TMP_Dropdown dropdown in Object.FindObjectsByType<TMP_Dropdown>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            PatchPersistentTargetType(dropdown, "m_OnValueChanged.m_PersistentCalls.m_Calls", oldTypeName, newTypeName);
    }

    private static void PatchUiPrefabPersistentTargetTypes()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Project/Prefabs/UI" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            try
            {
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    changed |= PatchPersistentTargetType(button, "m_OnClick.m_PersistentCalls.m_Calls", "Menu, Assembly-CSharp", "MainMenuController, Assembly-CSharp");
                    changed |= PatchPersistentTargetType(button, "m_OnClick.m_PersistentCalls.m_Calls", "GameOverController, Assembly-CSharp", "MatchResultsController, Assembly-CSharp");
                }

                foreach (Toggle toggle in root.GetComponentsInChildren<Toggle>(true))
                {
                    changed |= PatchPersistentTargetType(toggle, "onValueChanged.m_PersistentCalls.m_Calls", "Menu, Assembly-CSharp", "MainMenuController, Assembly-CSharp");
                    changed |= PatchPersistentTargetType(toggle, "onValueChanged.m_PersistentCalls.m_Calls", "GameOverController, Assembly-CSharp", "MatchResultsController, Assembly-CSharp");
                }

                foreach (Slider slider in root.GetComponentsInChildren<Slider>(true))
                {
                    changed |= PatchPersistentTargetType(slider, "m_OnValueChanged.m_PersistentCalls.m_Calls", "Menu, Assembly-CSharp", "MainMenuController, Assembly-CSharp");
                    changed |= PatchPersistentTargetType(slider, "m_OnValueChanged.m_PersistentCalls.m_Calls", "GameOverController, Assembly-CSharp", "MatchResultsController, Assembly-CSharp");
                }

                foreach (TMP_Dropdown dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
                {
                    changed |= PatchPersistentTargetType(dropdown, "m_OnValueChanged.m_PersistentCalls.m_Calls", "Menu, Assembly-CSharp", "MainMenuController, Assembly-CSharp");
                    changed |= PatchPersistentTargetType(dropdown, "m_OnValueChanged.m_PersistentCalls.m_Calls", "GameOverController, Assembly-CSharp", "MatchResultsController, Assembly-CSharp");
                }

                if (changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static bool PatchPersistentTargetType(Object target, string callsPropertyPath, string oldTypeName, string newTypeName)
    {
        bool changed = false;
        SerializedObject serialized = new(target);
        SerializedProperty calls = serialized.FindProperty(callsPropertyPath);
        if (calls == null)
            return false;

        for (int i = 0; i < calls.arraySize; i++)
        {
            SerializedProperty typeName = calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_TargetAssemblyTypeName");
            if (typeName != null && typeName.stringValue == oldTypeName)
            {
                typeName.stringValue = newTypeName;
                changed = true;
            }
        }

        if (!changed)
            return false;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
        return true;
    }

    private static void VerifyNoMissingScripts()
    {
        foreach (MonoBehaviour behaviour in Resources.FindObjectsOfTypeAll<MonoBehaviour>())
        {
            if (behaviour == null)
                throw new InvalidOperationException("A loaded scene or prefab contains a missing script.");
        }
    }

    private static void VerifyRenamedControllers()
    {
        if (Type.GetType("Menu, Assembly-CSharp") != null)
            throw new InvalidOperationException("Old Menu class still exists.");
        if (Type.GetType("GameOverController, Assembly-CSharp") != null)
            throw new InvalidOperationException("Old GameOverController class still exists.");
        if (Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include) == null)
            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
    }

    private static void VerifyLobbyRoots()
    {
        VerifyLobbyRoot(SingleplayerLobbyRootPrefabPath, "SINGLEPLAYER");
        VerifyLobbyRoot(MultiplayerLobbyRootPrefabPath, "MULTIPLAYER");

        EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
        if (Object.FindObjectsByType<LobbyRootSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("LobbyScene must contain exactly one LobbyRootSelector.");
    }

    private static void VerifyLobbyRoot(string path, string expectedTitle)
    {
        GameObject prefab = LoadAsset<GameObject>(path);
        TMP_Text title = FindTextByName(prefab.transform, "LobbyText (TMP)");
        if (title == null || title.text != expectedTitle)
            throw new InvalidOperationException($"{path} title must be {expectedTitle}.");

        LobbyController[] controllers = prefab.GetComponentsInChildren<LobbyController>(true);
        if (controllers.Length != 1)
            throw new InvalidOperationException($"{path} must contain exactly one LobbyController.");
    }

    private static void VerifyCoreSelectables()
    {
        foreach (string path in CoreSelectablePrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            foreach (Selectable selectable in prefab.GetComponentsInChildren<Selectable>(true))
            {
                if (selectable.GetComponent<MenuSelectable>() == null)
                    throw new InvalidOperationException($"{path}/{selectable.name} is missing MenuSelectable.");
            }
        }
    }

    private static void VerifyGameOverScene()
    {
        EditorSceneManager.OpenScene(GameOverScenePath, OpenSceneMode.Single);
        if (Object.FindFirstObjectByType<MatchResultsController>(FindObjectsInactive.Include) == null)
            throw new InvalidOperationException("GameOver scene is missing MatchResultsController.");
        TMP_Text title = FindTextByName(null, "TitleText (TMP)");
        if (title == null || title.text != "MATCH RESULTS")
            throw new InvalidOperationException("Match results title is not set.");
    }

    private static void RenameObject(string oldName, string newName)
    {
        Transform transform = FindTransformByName(oldName);
        if (transform != null)
            transform.name = newName;
    }

    private static void ReplaceExactText(string oldText, string newText)
    {
        foreach (TMP_Text text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (text.text == oldText)
                text.text = newText;
        }
    }

    private static void SetTextByName(string name, string value)
    {
        TMP_Text text = FindTextByName(null, name);
        if (text != null)
            text.text = value;
    }

    private static void SetText(GameObject root, string name, string value)
    {
        TMP_Text text = FindTextByName(root.transform, name);
        if (text != null)
            text.text = value;
    }

    private static TMP_Text CreateText(Transform parent, string name)
    {
        GameObject textObject = new(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        return textObject.GetComponent<TMP_Text>();
    }

    private static TMP_Text FindTextByName(Transform root, string name)
    {
        TMP_Text[] texts = root != null
            ? root.GetComponentsInChildren<TMP_Text>(true)
            : Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (TMP_Text text in texts)
        {
            if (text.name == name)
                return text;
        }

        return null;
    }

    private static RectTransform FindRect(string name)
    {
        Transform transform = FindTransformByName(name);
        return transform as RectTransform;
    }

    private static Transform FindTransformByName(string name)
    {
        foreach (Transform transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform.name == name)
                return transform;
        }

        return null;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child;
        }

        return null;
    }

    private static Button FindButtonByName(string name)
    {
        foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.name == name)
                return button;
        }

        return null;
    }

    private static void SetButtonLabel(GameObject buttonRoot, string value)
    {
        TMP_Text text = buttonRoot.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = value;
    }

    private static Image EnsureImage(Transform parent, string name)
    {
        Transform existing = FindDirectChild(parent, name);
        GameObject imageObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ReplacePersistentClick(Button button, UnityAction action)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);

        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static T LoadAsset<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            throw new InvalidOperationException($"Missing required asset at {path}");

        return asset;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            return;

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string name = Path.GetFileName(folderPath);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
