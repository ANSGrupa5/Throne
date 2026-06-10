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
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class Milestone72LobbyVisualSplitPatcher
{
    private const string LobbyScenePath = "Assets/Project/Scenes/UI/Lobby/LobbyScene.unity";
    private const string MultiplayerConnectionScenePath = "Assets/Project/Scenes/UI/Menu/MultiplayerConnection.unity";
    private const string GameOverScenePath = "Assets/Project/Scenes/UI/Menu/GameOver.unity";

    private const string SourceLobbyRootPrefabPath = "Assets/Project/Prefabs/UI/Lobby/LobbyRoot.prefab";
    private const string SingleplayerLobbyRootPrefabPath = "Assets/Project/Prefabs/UI/Lobby/SingleplayerLobbyRoot.prefab";
    private const string MultiplayerLobbyRootPrefabPath = "Assets/Project/Prefabs/UI/Lobby/MultiplayerLobbyRoot.prefab";
    private const string MenuButtonPrefabPath = "Assets/Project/Prefabs/UI/Menu/MenuButton.prefab";
    private const string MenuBackButtonPrefabPath = "Assets/Project/Prefabs/UI/Menu/MenuBackButton.prefab";
    private const string MenuDropdownPrefabPath = "Assets/Project/Prefabs/UI/Menu/MenuDropdown.prefab";

    private const string DefaultVisualPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Default.asset";
    private const string DarkerVisualPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Darker.asset";
    private const string DangerVisualPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Danger.asset";
    private const string TransparentVisualPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_Transparent.asset";
    private const string DisabledSlotVisualPresetPath = "Assets/Project/Data/UI/MenuSelectable/MenuSelectable_DisabledSlot.asset";
    private const string MenuDropdownSoundPath = "Assets/Project/Art/Audio/menu_dropdown.wav";
    private const string MenuClickSoundPath = "Assets/Project/Art/Audio/menu_click.wav";

    private static readonly string[] CoreMenuPrefabPaths =
    {
        "Assets/Project/Prefabs/UI/Menu/MenuButton.prefab",
        "Assets/Project/Prefabs/UI/Menu/MenuBackButton.prefab",
        "Assets/Project/Prefabs/UI/Menu/MenuSelectorButton.prefab",
        "Assets/Project/Prefabs/UI/Menu/MenuToggle.prefab",
        "Assets/Project/Prefabs/UI/Menu/MenuSlider.prefab",
        "Assets/Project/Prefabs/UI/Menu/MenuDropdown.prefab",
        "Assets/Project/Prefabs/UI/Lobby/TrailColorButton.prefab",
        "Assets/Project/Prefabs/UI/Lobby/OpponentSlot.prefab"
    };

    private static readonly string[] ConnectionOnlyObjectNames =
    {
        "ConnectionType",
        "JoinPanel",
        "JoinPopup",
        "AddressInput",
        "NetworkStatus",
        "LobbyStatusTitle"
    };

    [MenuItem("Throne/Tools/Migrations/Run Milestone 7.2 Lobby Visual Split")]
    public static void Run()
    {
        EnsureRoleLobbyPrefabs();
        PatchVisualPresets();
        PatchCoreMenuPrefabs();
        PatchLobbyScene();
        PatchMultiplayerConnectionScene();
        PatchGameOverScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Milestone72LobbyVisualSplitPatcher] Patch complete.");
    }

    [MenuItem("Throne/Tools/Migrations/Verify Milestone 7.2 Lobby Visual Split")]
    public static void Verify()
    {
        VerifyLobbyRootPrefab(SingleplayerLobbyRootPrefabPath, "SOLO RUN SETUP", LobbyMode.Singleplayer);
        VerifyLobbyRootPrefab(MultiplayerLobbyRootPrefabPath, "NETWORK LOBBY", LobbyMode.MultiplayerClient);
        VerifyLobbyScene();
        VerifyGameOverScene();
        VerifyCoreMenuPrefabs();
        VerifyDropdownFeedback();
        Debug.Log("[Milestone72LobbyVisualSplitPatcher] Verification complete.");
    }

    public static void RunAndVerify()
    {
        Run();
        Verify();
    }

    private static void EnsureRoleLobbyPrefabs()
    {
        EnsureCopiedPrefab(SourceLobbyRootPrefabPath, SingleplayerLobbyRootPrefabPath);
        EnsureCopiedPrefab(SourceLobbyRootPrefabPath, MultiplayerLobbyRootPrefabPath);

        PatchLobbyRootPrefab(SingleplayerLobbyRootPrefabPath, false);
        PatchLobbyRootPrefab(MultiplayerLobbyRootPrefabPath, true);
    }

    private static void EnsureCopiedPrefab(string sourcePath, string targetPath)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(targetPath) != null)
            return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
            throw new InvalidOperationException($"Missing source prefab at {sourcePath}");

        EnsureFolder(Path.GetDirectoryName(targetPath)?.Replace('\\', '/'));
        if (!AssetDatabase.CopyAsset(sourcePath, targetPath))
            throw new InvalidOperationException($"Failed to copy {sourcePath} to {targetPath}");
    }

    private static void PatchLobbyRootPrefab(string prefabPath, bool multiplayer)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            root.name = multiplayer ? "MultiplayerLobbyRoot" : "SingleplayerLobbyRoot";
            PatchLobbyController(root, multiplayer);
            RemoveMultiplayerConnectionMenu(root);
            RemoveConnectionOnlyObjects(root);
            PatchLobbyTexts(root, multiplayer);
            PatchLobbyLighting(root, multiplayer);
            PatchDropdowns(root);
            PatchTrailColorButtonAlphas(root);
            NormalizeSelectableTargets(root);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void PatchLobbyController(GameObject root, bool multiplayer)
    {
        LobbyController lobby = root.GetComponentInChildren<LobbyController>(true);
        if (lobby == null)
            throw new InvalidOperationException($"{root.name} is missing {nameof(LobbyController)}.");

        SerializedObject serialized = new(lobby);
        serialized.FindProperty("configuredLobbyMode").enumValueIndex =
            multiplayer ? (int)LobbyMode.MultiplayerClient : (int)LobbyMode.Singleplayer;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(lobby);
    }

    private static void RemoveMultiplayerConnectionMenu(GameObject root)
    {
        foreach (MultiplayerConnectionMenu menu in root.GetComponentsInChildren<MultiplayerConnectionMenu>(true))
            Object.DestroyImmediate(menu);
    }

    private static void RemoveConnectionOnlyObjects(GameObject root)
    {
        foreach (string objectName in ConnectionOnlyObjectNames)
        {
            Transform child = FindDeepChild(root.transform, objectName);
            if (child != null)
                Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void PatchLobbyTexts(GameObject root, bool multiplayer)
    {
        SetText(root, "LobbyText (TMP)", multiplayer ? "NETWORK LOBBY" : "SOLO RUN SETUP");
        SetText(root, "PlayersText (TMP)", multiplayer ? "PLAYERS" : "BOTS");
        SetText(root, "TitleText (TMP)", "THRONE");
    }

    private static void PatchLobbyLighting(GameObject root, bool multiplayer)
    {
        foreach (Light light in root.GetComponentsInChildren<Light>(true))
        {
            if (light.type != LightType.Directional)
                continue;

            if (multiplayer)
            {
                light.color = new Color(0.46f, 0.56f, 1f, 1f);
                light.intensity = 0.65f;
                light.transform.localRotation = Quaternion.Euler(28f, 150f, 0f);
            }
            else
            {
                light.color = new Color(0.72f, 0.95f, 1f, 1f);
                light.intensity = 0.95f;
                light.transform.localRotation = Quaternion.Euler(38f, -35f, 0f);
            }
        }

        foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
        {
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = multiplayer
                ? new Color(0.004f, 0.008f, 0.02f, 1f)
                : new Color(0.01f, 0.025f, 0.035f, 1f);
        }

        Transform scrim = FindDeepChild(root.transform, "ReadabilityScrim");
        if (scrim != null && scrim.TryGetComponent(out Image scrimImage))
        {
            scrimImage.color = multiplayer
                ? new Color(0f, 0.012f, 0.035f, 0.24f)
                : new Color(0f, 0.02f, 0.035f, 0.14f);
        }
    }

    private static void PatchVisualPresets()
    {
        MenuSelectableVisualPreset preset = LoadAsset<MenuSelectableVisualPreset>(DefaultVisualPresetPath);
        preset.NormalColor = new Color(0f, 0.996f, 0.925f, 1f);
        preset.HighlightedColor = new Color(0.9f, 1f, 0.98f, 1f);
        preset.PressedColor = new Color(0f, 0.64f, 0.72f, 1f);
        preset.SelectedColor = new Color(0.9f, 1f, 0.98f, 1f);
        preset.DisabledColor = new Color(0f, 0.996f, 0.925f, 0.5f);
        preset.ColorMultiplier = 1f;
        EditorUtility.SetDirty(preset);

        SetColorMultiplier(DarkerVisualPresetPath, 1f);
        SetColorMultiplier(DangerVisualPresetPath, 1f);
        SetColorMultiplier(TransparentVisualPresetPath, 1f);
        SetColorMultiplier(DisabledSlotVisualPresetPath, 1f);
    }

    private static void PatchCoreMenuPrefabs()
    {
        foreach (string path in CoreMenuPrefabPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                NormalizeSelectableTargets(root);
                PatchDropdowns(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static void NormalizeSelectableTargets(GameObject root)
    {
        foreach (Selectable selectable in root.GetComponentsInChildren<Selectable>(true))
        {
            ColorBlock colors = selectable.colors;
            colors.colorMultiplier = 1f;
            selectable.colors = colors;

            MenuSelectable menuSelectable = selectable.GetComponent<MenuSelectable>();
            if (menuSelectable == null)
                continue;

            Graphic target = ResolveTintTarget(selectable);
            SerializedObject serialized = new(menuSelectable);
            SerializedProperty targetProperty = serialized.FindProperty("targetGraphicOverride");
            if (targetProperty.objectReferenceValue == null || !IsValidTintTarget(targetProperty.objectReferenceValue as Graphic))
                targetProperty.objectReferenceValue = target;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(menuSelectable);

            if (target != null && menuSelectable.name != "MenuBackButton")
            {
                Color targetColor = target.color;
                if (targetColor.r < 0.08f && targetColor.g < 0.08f && targetColor.b < 0.08f)
                {
                    target.color = new Color(1f, 1f, 1f, targetColor.a);
                    EditorUtility.SetDirty(target);
                }
            }
        }
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

        string lowerName = graphic.name.ToLowerInvariant();
        return !lowerName.Contains("icon") &&
               !lowerName.Contains("checkmark") &&
               !lowerName.Contains("text") &&
               !lowerName.Contains("label");
    }

    private static void PatchDropdowns(GameObject root)
    {
        AudioClip openSound = LoadAsset<AudioClip>(MenuDropdownSoundPath);
        AudioClip clickSound = LoadAsset<AudioClip>(MenuClickSoundPath);

        foreach (TMP_Dropdown dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
        {
            MenuDropdownFeedback feedback = dropdown.GetComponent<MenuDropdownFeedback>();
            if (feedback == null)
                feedback = dropdown.gameObject.AddComponent<MenuDropdownFeedback>();

            SerializedObject serializedFeedback = new(feedback);
            serializedFeedback.FindProperty("dropdown").objectReferenceValue = dropdown;
            serializedFeedback.FindProperty("openSound").objectReferenceValue = openSound;
            serializedFeedback.FindProperty("selectSound").objectReferenceValue = clickSound;
            serializedFeedback.FindProperty("openVolume").floatValue = 1.35f;
            serializedFeedback.FindProperty("selectVolume").floatValue = 1f;
            serializedFeedback.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feedback);

            MenuSelectable menuSelectable = dropdown.GetComponent<MenuSelectable>();
            if (menuSelectable != null)
            {
                SerializedObject serializedSelectable = new(menuSelectable);
                serializedSelectable.FindProperty("playClickSound").boolValue = false;
                serializedSelectable.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(menuSelectable);
            }
        }
    }

    private static void PatchTrailColorButtonAlphas(GameObject root)
    {
        foreach (LobbyController lobby in root.GetComponentsInChildren<LobbyController>(true))
        {
            SerializedObject serialized = new(lobby);
            SerializedProperty trailButtons = serialized.FindProperty("trailColorButtons");
            for (int i = 0; i < trailButtons.arraySize; i++)
            {
                SerializedProperty button = trailButtons.GetArrayElementAtIndex(i);
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

        foreach (LobbyRootSelector existingSelector in Object.FindObjectsByType<LobbyRootSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(existingSelector.gameObject);

        HashSet<GameObject> lobbyRootsToDelete = new();
        foreach (LobbyController controller in Object.FindObjectsByType<LobbyController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject root = controller.transform.root.gameObject;
            if (root.GetComponent<LobbyRootSelector>() == null)
                lobbyRootsToDelete.Add(root);
        }

        foreach (GameObject root in lobbyRootsToDelete)
            Object.DestroyImmediate(root);

        GameObject selectorObject = new("LobbyRootSelector");
        SceneManager.MoveGameObjectToScene(selectorObject, scene);
        LobbyRootSelector selectorComponent = selectorObject.AddComponent<LobbyRootSelector>();

        GameObject singleplayerRoot = InstantiatePrefabInScene(SingleplayerLobbyRootPrefabPath, scene, selectorObject.transform, "SingleplayerLobbyRoot");
        GameObject multiplayerRoot = InstantiatePrefabInScene(MultiplayerLobbyRootPrefabPath, scene, selectorObject.transform, "MultiplayerLobbyRoot");

        ConfigureLobbySceneRoot(singleplayerRoot);
        ConfigureLobbySceneRoot(multiplayerRoot);
        singleplayerRoot.SetActive(false);
        multiplayerRoot.SetActive(false);

        SerializedObject serializedSelector = new(selectorComponent);
        serializedSelector.FindProperty("fallbackMode").enumValueIndex = (int)LobbyMode.Singleplayer;
        serializedSelector.FindProperty("singleplayerRoot").objectReferenceValue = singleplayerRoot;
        serializedSelector.FindProperty("multiplayerRoot").objectReferenceValue = multiplayerRoot;
        serializedSelector.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(selectorComponent);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static GameObject InstantiatePrefabInScene(string prefabPath, Scene scene, Transform parent, string objectName)
    {
        GameObject prefab = LoadAsset<GameObject>(prefabPath);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (instance == null)
            throw new InvalidOperationException($"Failed to instantiate {prefabPath}.");

        instance.name = objectName;
        instance.transform.SetParent(parent, false);
        return instance;
    }

    private static void ConfigureLobbySceneRoot(GameObject root)
    {
        root.transform.localPosition = new Vector3(-33f, 0f, 900f);
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
    }

    private static void PatchMultiplayerConnectionScene()
    {
        Scene scene = EditorSceneManager.OpenScene(MultiplayerConnectionScenePath, OpenSceneMode.Single);
        MultiplayerConnectionMenu menu = Object.FindFirstObjectByType<MultiplayerConnectionMenu>(FindObjectsInactive.Include);
        if (menu == null)
            throw new InvalidOperationException("MultiplayerConnection scene is missing MultiplayerConnectionMenu.");

        Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
            throw new InvalidOperationException("MultiplayerConnection scene is missing Canvas.");

        AddOrPatchConnectionBackground(canvas.transform);
        AddOrPatchConnectionBackButton(canvas.transform, menu);
        PatchConnectionCards(menu.transform);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AddOrPatchConnectionBackground(Transform canvas)
    {
        Transform background = FindDirectChild(canvas, "CyberConnectionBackground");
        GameObject backgroundObject = background != null ? background.gameObject : new GameObject("CyberConnectionBackground", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(canvas, false);
        backgroundObject.transform.SetAsFirstSibling();

        RectTransform rect = backgroundObject.GetComponent<RectTransform>();
        StretchToParent(rect);

        Image image = backgroundObject.GetComponent<Image>();
        image.color = new Color(0.005f, 0.018f, 0.028f, 1f);
        image.raycastTarget = false;
    }

    private static void AddOrPatchConnectionBackButton(Transform canvas, MultiplayerConnectionMenu menu)
    {
        Button backButton = FindButtonByName(canvas, "MainBackButton");
        if (backButton == null)
        {
            GameObject prefab = LoadAsset<GameObject>(MenuBackButtonPrefabPath);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab, canvas) as GameObject;
            if (instance == null)
                throw new InvalidOperationException("Failed to instantiate MenuBackButton.");

            instance.name = "MainBackButton";
            backButton = instance.GetComponent<Button>();
        }

        RectTransform rect = backButton.transform as RectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(36f, -32f);
        rect.sizeDelta = new Vector2(190f, 58f);

        SetButtonLabel(backButton.gameObject, "BACK");
        ReplacePersistentClick(backButton, menu.BackToMainMenu);
    }

    private static void PatchConnectionCards(Transform sceneRoot)
    {
        Transform connectionPanel = FindDeepChild(sceneRoot, "ConnectionType");
        if (connectionPanel == null)
            return;

        Button hostButton = FindButtonByName(connectionPanel, "HostGameButton");
        Button joinButton = FindButtonByName(connectionPanel, "JoinGameButton");

        GameObject hostCard = EnsureCard(connectionPanel, "HostSessionCard", new Vector2(-310f, -40f), "HOST SESSION", "Create a lobby and wait for riders.");
        GameObject joinCard = EnsureCard(connectionPanel, "JoinSessionCard", new Vector2(310f, -40f), "JOIN SESSION", "Connect to a host by address.");

        if (hostButton != null)
            MoveButtonIntoCard(hostButton, hostCard.transform, "HOST SESSION");
        if (joinButton != null)
            MoveButtonIntoCard(joinButton, joinCard.transform, "JOIN SESSION");
    }

    private static GameObject EnsureCard(Transform parent, string name, Vector2 position, string title, string body)
    {
        Transform existing = FindDirectChild(parent, name);
        GameObject card = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        card.transform.SetParent(parent, false);

        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(500f, 310f);

        Image image = card.GetComponent<Image>();
        image.color = new Color(0.015f, 0.05f, 0.065f, 0.9f);

        Outline outline = card.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0.996f, 0.925f, 0.34f);
        outline.effectDistance = new Vector2(2f, -2f);

        EnsureCardText(card.transform, "Title", title, new Vector2(0f, 94f), 34f, FontStyles.Bold);
        EnsureCardText(card.transform, "Body", body, new Vector2(0f, 36f), 20f, FontStyles.Normal);
        return card;
    }

    private static void EnsureCardText(Transform parent, string name, string text, Vector2 position, float size, FontStyles style)
    {
        Transform existing = FindDirectChild(parent, name);
        GameObject textObject = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(430f, 60f);

        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = style == FontStyles.Bold
            ? new Color(0.9f, 1f, 0.98f, 1f)
            : new Color(0.75f, 0.9f, 0.92f, 1f);
        label.raycastTarget = false;
    }

    private static void MoveButtonIntoCard(Button button, Transform card, string label)
    {
        button.transform.SetParent(card, false);
        RectTransform rect = button.transform as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -94f);
        rect.sizeDelta = new Vector2(340f, 72f);
        SetButtonLabel(button.gameObject, label);
    }

    private static void PatchGameOverScene()
    {
        Scene scene = EditorSceneManager.OpenScene(GameOverScenePath, OpenSceneMode.Single);
        GameOverController controller = Object.FindFirstObjectByType<GameOverController>(FindObjectsInactive.Include);
        if (controller == null)
            throw new InvalidOperationException("GameOver scene is missing GameOverController.");

        Transform panel = FindDeepChild(controller.transform, "GameOverPanel") ?? controller.transform;
        Button mainMenuButton = FindButtonByName(panel, "ReturnToMainMenuButton") ?? FindButtonByName(panel, "BackButton");
        if (mainMenuButton == null)
            mainMenuButton = InstantiateMenuButton(panel, "ReturnToMainMenuButton");

        mainMenuButton.name = "ReturnToMainMenuButton";
        ConfigureGameOverButton(mainMenuButton, new Vector2(-155f, -260f), "MAIN MENU");
        ReplacePersistentClick(mainMenuButton, controller.ReturnToMainMenu);

        Button lobbyButton = FindButtonByName(panel, "ReturnToLobbyButton");
        if (lobbyButton == null)
            lobbyButton = InstantiateMenuButton(panel, "ReturnToLobbyButton");

        ConfigureGameOverButton(lobbyButton, new Vector2(155f, -260f), "LOBBY");
        ReplacePersistentClick(lobbyButton, controller.ReturnToLobby);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Button InstantiateMenuButton(Transform parent, string name)
    {
        GameObject prefab = LoadAsset<GameObject>(MenuButtonPrefabPath);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (instance == null)
            throw new InvalidOperationException($"Failed to instantiate {MenuButtonPrefabPath}.");

        instance.name = name;
        return instance.GetComponent<Button>();
    }

    private static void ConfigureGameOverButton(Button button, Vector2 position, string label)
    {
        RectTransform rect = button.transform as RectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(270f, 68f);
        SetButtonLabel(button.gameObject, label);
    }

    private static void VerifyLobbyRootPrefab(string prefabPath, string expectedHeader, LobbyMode expectedMode)
    {
        GameObject prefab = LoadAsset<GameObject>(prefabPath);
        LobbyController[] controllers = prefab.GetComponentsInChildren<LobbyController>(true);
        if (controllers.Length != 1)
            throw new InvalidOperationException($"{prefabPath} must contain exactly one LobbyController, found {controllers.Length}.");

        if (prefab.GetComponentInChildren<MultiplayerConnectionMenu>(true) != null)
            throw new InvalidOperationException($"{prefabPath} must not contain MultiplayerConnectionMenu.");

        SerializedObject lobby = new(controllers[0]);
        if (lobby.FindProperty("configuredLobbyMode").enumValueIndex != (int)expectedMode)
            throw new InvalidOperationException($"{prefabPath} has the wrong configured lobby mode.");

        TMP_Text header = FindTextByName(prefab.transform, "LobbyText (TMP)");
        if (header == null || header.text != expectedHeader)
            throw new InvalidOperationException($"{prefabPath} header is not '{expectedHeader}'.");

        if (prefab.GetComponentInChildren<Light>(true) == null)
            throw new InvalidOperationException($"{prefabPath} is missing lobby lighting.");
    }

    private static void VerifyLobbyScene()
    {
        Scene scene = EditorSceneManager.OpenScene(LobbyScenePath, OpenSceneMode.Single);
        LobbyRootSelector[] selectors = Object.FindObjectsByType<LobbyRootSelector>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (selectors.Length != 1)
            throw new InvalidOperationException($"LobbyScene must contain exactly one LobbyRootSelector, found {selectors.Length}.");

        SerializedObject selector = new(selectors[0]);
        if (selector.FindProperty("singleplayerRoot").objectReferenceValue == null ||
            selector.FindProperty("multiplayerRoot").objectReferenceValue == null)
        {
            throw new InvalidOperationException("LobbyRootSelector is missing role root references.");
        }

        LobbyController[] controllers = Object.FindObjectsByType<LobbyController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (controllers.Length != 2)
            throw new InvalidOperationException($"LobbyScene should contain two inactive role LobbyControllers, found {controllers.Length}.");
    }

    private static void VerifyGameOverScene()
    {
        EditorSceneManager.OpenScene(GameOverScenePath, OpenSceneMode.Single);
        GameOverController controller = Object.FindFirstObjectByType<GameOverController>(FindObjectsInactive.Include);
        if (controller == null)
            throw new InvalidOperationException("GameOver scene is missing GameOverController.");

        VerifyButtonPersistentCall("ReturnToMainMenuButton", "ReturnToMainMenu");
        VerifyButtonPersistentCall("ReturnToLobbyButton", "ReturnToLobby");
    }

    private static void VerifyCoreMenuPrefabs()
    {
        foreach (string path in CoreMenuPrefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            foreach (MenuSelectable selectable in prefab.GetComponentsInChildren<MenuSelectable>(true))
            {
                SerializedObject serialized = new(selectable);
                bool useColorTint = serialized.FindProperty("useColorTint").boolValue;
                Object target = serialized.FindProperty("targetGraphicOverride").objectReferenceValue;
                if (useColorTint && target == null)
                    throw new InvalidOperationException($"{path} has a MenuSelectable without targetGraphicOverride.");
            }
        }
    }

    private static void VerifyDropdownFeedback()
    {
        foreach (string path in new[] { MenuDropdownPrefabPath, SingleplayerLobbyRootPrefabPath, MultiplayerLobbyRootPrefabPath })
        {
            GameObject prefab = LoadAsset<GameObject>(path);
            foreach (TMP_Dropdown dropdown in prefab.GetComponentsInChildren<TMP_Dropdown>(true))
            {
                MenuDropdownFeedback feedback = dropdown.GetComponent<MenuDropdownFeedback>();
                if (feedback == null)
                    throw new InvalidOperationException($"{path} dropdown is missing MenuDropdownFeedback.");

                SerializedObject serializedFeedback = new(feedback);
                if (serializedFeedback.FindProperty("openSound").objectReferenceValue == null ||
                    serializedFeedback.FindProperty("selectSound").objectReferenceValue == null)
                    throw new InvalidOperationException($"{path} dropdown feedback is missing audio clips.");

                MenuSelectable menuSelectable = dropdown.GetComponent<MenuSelectable>();
                if (menuSelectable != null)
                {
                    SerializedObject serializedSelectable = new(menuSelectable);
                    if (serializedSelectable.FindProperty("playClickSound").boolValue)
                        throw new InvalidOperationException($"{path} dropdown root still plays generic click sound.");
                }
            }
        }
    }

    private static void VerifyButtonPersistentCall(string buttonName, string methodName)
    {
        Button button = FindButtonByName(null, buttonName);
        if (button == null)
            throw new InvalidOperationException($"GameOver scene is missing {buttonName}.");

        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
        {
            if (button.onClick.GetPersistentMethodName(i) == methodName)
                return;
        }

        throw new InvalidOperationException($"{buttonName} must call {methodName}.");
    }

    private static void SetColorMultiplier(string path, float value)
    {
        MenuSelectableVisualPreset preset = AssetDatabase.LoadAssetAtPath<MenuSelectableVisualPreset>(path);
        if (preset == null)
            return;

        preset.ColorMultiplier = value;
        EditorUtility.SetDirty(preset);
    }

    private static void SetText(GameObject root, string name, string text)
    {
        TMP_Text label = FindTextByName(root.transform, name);
        if (label == null)
            return;

        label.text = text;
        EditorUtility.SetDirty(label);
    }

    private static TMP_Text FindTextByName(Transform root, string name)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name == name)
                return text;
        }

        return null;
    }

    private static void SetButtonLabel(GameObject buttonRoot, string label)
    {
        TMP_Text text = buttonRoot.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
            EditorUtility.SetDirty(text);
        }
    }

    private static void ReplacePersistentClick(Button button, UnityAction action)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);

        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static Button FindButtonByName(Transform root, string name)
    {
        Button[] buttons = root != null
            ? root.GetComponentsInChildren<Button>(true)
            : Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Button button in buttons)
        {
            if (button.name == name)
                return button;
        }

        return null;
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        if (root == null)
            return null;

        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
                return child;
        }

        return null;
    }

    private static Transform FindDirectChild(Transform parent, string name)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child;
        }

        return null;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
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
