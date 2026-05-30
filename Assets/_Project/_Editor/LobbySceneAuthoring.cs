#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class LobbySceneAuthoring
{
    private const int LobbySlotCount = 6;
    private const string ButtonPrefabPath = "Assets/_Project/_Prefabs/Menu/Button.prefab";
    private const string PlayerSlotPrefabPath = "Assets/_Project/_UI/Prefabs/PlayerSlot.prefab";
    private const string MultiplayerConnectionScenePath = "Assets/_Project/_Scenes/MultiplayerConnection.unity";
    private const string MultiplayerConnectionSceneName = "MultiplayerConnection";
    private const string MultiplayerLobbySceneName = "MultiplayerLobby";

    private static readonly string[] TrailColorButtonNames =
    {
        "RedColorButton",
        "BlueColorButton",
        "GreenColorButton",
        "YellowColorButton",
        "MagentaColorButton",
        "CyanColorButton"
    };

    public static void Apply()
    {
        AuthorScene("Assets/_Project/_Scenes/MultiplayerLobby.unity");
        AuthorScene("Assets/_Project/_Scenes/SingleplayerLobby.unity");
        AuthorMultiplayerConnectionScene();
        UpdateMainMenuScene();
        EnsureSceneInBuildSettings(MultiplayerConnectionScenePath);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Throne/Author Lobby Scene UI References")]
    public static void ApplyFromMenu()
    {
        Apply();
    }

    private static void AuthorScene(string scenePath)
    {
        Scene scene = EditorSceneManager.OpenScene(scenePath);

        MatchLobbyController lobby = FindSceneComponent<MatchLobbyController>(scene);
        if (lobby != null)
            AuthorLobbyReferences(scene, lobby);

        MultiplayerConnectionMenu multiplayerMenu = FindSceneComponent<MultiplayerConnectionMenu>(scene);
        if (multiplayerMenu != null)
            AuthorMultiplayerMenu(scene, multiplayerMenu);

        RemoveButtonEventTriggers(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void AuthorMultiplayerMenu(Scene scene, MultiplayerConnectionMenu multiplayerMenu)
    {
        Canvas canvas = multiplayerMenu.GetComponent<Canvas>() ?? multiplayerMenu.GetComponentInParent<Canvas>(true);
        if (canvas == null)
            return;

        Button buttonPrefab = AssetDatabase.LoadAssetAtPath<Button>(ButtonPrefabPath);
        Transform canvasTransform = canvas.transform;

        GameObject connectionTypePanel = FindSceneGameObject(scene, "ConnectionType") ??
            FindSceneGameObject(scene, "ConnectionTypePanel");
        GameObject lobbyPanel = FindSceneGameObject(scene, "Panel") ??
            FindSceneGameObject(scene, "HostPanel");

        GameObject joinPanel = FindSceneGameObject(scene, "JoinPanel") ??
            CreatePanel("JoinPanel", canvasTransform, Vector2.zero, new Vector2(320f, 220f));
        joinPanel.SetActive(false);

        TMP_Text joinTitle = EnsureText("JoinTitle", joinPanel.transform, "JOIN GAME", new Vector2(0f, 74f), new Vector2(300f, 40f), 30f);
        joinTitle.fontStyle = FontStyles.UpperCase;

        TMP_InputField addressInput = EnsureInput("AddressInput", joinPanel.transform, "127.0.0.1", new Vector2(0f, 20f), new Vector2(260f, 42f));
        Button confirmJoinButton = EnsureButton("ConfirmJoinButton", joinPanel.transform, buttonPrefab, "JOIN", new Vector2(0f, -44f), new Vector2(220f, 56f));
        Button backJoinButton = EnsureButton("BackJoinButton", joinPanel.transform, buttonPrefab, "BACK", new Vector2(0f, -108f), new Vector2(220f, 56f));

        TMP_Text statusText = EnsureText("NetworkStatus", canvasTransform, string.Empty, new Vector2(0f, -250f), new Vector2(420f, 34f), 20f);

        GameObject popupPanel = FindSceneGameObject(scene, "JoinPopup") ??
            CreatePanel("JoinPopup", canvasTransform, Vector2.zero, new Vector2(420f, 170f));
        popupPanel.SetActive(false);

        Image popupBackground = popupPanel.GetComponent<Image>() ?? popupPanel.AddComponent<Image>();
        popupBackground.color = new Color(0f, 0.06f, 0.08f, 0.92f);
        popupBackground.raycastTarget = true;

        TMP_Text popupText = EnsureText("PopupText", popupPanel.transform, string.Empty, new Vector2(0f, 34f), new Vector2(380f, 62f), 22f);
        Button popupOkButton = EnsureButton("PopupOkButton", popupPanel.transform, buttonPrefab, "OK", new Vector2(0f, -48f), new Vector2(220f, 56f));

        SerializedObject serializedMenu = new(multiplayerMenu);
        SetObject(serializedMenu, "connectionTypePanel", connectionTypePanel);
        SetObject(serializedMenu, "lobbyPanel", lobbyPanel);
        SetObject(serializedMenu, "joinPanel", joinPanel);
        SetObject(serializedMenu, "joinAddressInput", addressInput);
        SetObject(serializedMenu, "statusText", statusText);
        SetObject(serializedMenu, "popupPanel", popupPanel);
        SetObject(serializedMenu, "popupText", popupText);
        SetObject(serializedMenu, "confirmJoinButton", confirmJoinButton);
        SetObject(serializedMenu, "backJoinButton", backJoinButton);
        SetObject(serializedMenu, "popupOkButton", popupOkButton);
        MatchLobbyController sceneLobby = FindSceneComponent<MatchLobbyController>(scene);
        SetString(serializedMenu, "lobbySceneName", MultiplayerLobbySceneName);
        SetObject(serializedMenu, "matchLobby", sceneLobby);
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();

        if (sceneLobby != null)
        {
            if (connectionTypePanel != null)
                connectionTypePanel.SetActive(false);
            if (lobbyPanel != null)
                lobbyPanel.SetActive(true);
        }
        else if (connectionTypePanel != null)
        {
            connectionTypePanel.SetActive(true);
        }
    }

    private static void AuthorLobbyReferences(Scene scene, MatchLobbyController lobby)
    {
        SerializedObject serializedLobby = new(lobby);

        TMP_Text heading = FindSceneGameObject(scene, "PlayersText (TMP)")?.GetComponent<TMP_Text>();
        SetObject(serializedLobby, "playerHeading", heading);

        AssignTrailColorButtons(scene, serializedLobby);
        AssignLobbySlots(scene, serializedLobby);

        serializedLobby.ApplyModifiedPropertiesWithoutUndo();

        Button playButton = FindSceneGameObject(scene, "PlayButton")?.GetComponent<Button>();
        SetPersistentButtonCall(playButton, lobby.StartMatch);

        Button backButton = FindSceneGameObject(scene, "BackButton")?.GetComponent<Button>();
        SetPersistentButtonCall(backButton, lobby.BackToMainMenu);
    }

    private static void AssignTrailColorButtons(Scene scene, SerializedObject serializedLobby)
    {
        SerializedProperty buttons = serializedLobby.FindProperty("trailColorButtons");
        buttons.arraySize = TrailColorButtonNames.Length;

        for (int i = 0; i < TrailColorButtonNames.Length; i++)
        {
            GameObject buttonObject = FindSceneGameObject(scene, TrailColorButtonNames[i]);
            Button button = buttonObject != null ? buttonObject.GetComponent<Button>() : null;
            Image colorImage = buttonObject != null ? buttonObject.GetComponent<Image>() : null;
            GameObject frame = buttonObject != null ? EnsureTrailColorFrame(buttonObject.transform) : null;

            SerializedProperty element = buttons.GetArrayElementAtIndex(i);
            SetRelativeObject(element, "button", button);
            SetRelativeObject(element, "colorImage", colorImage);
            SetRelativeObject(element, "selectionFrame", frame);
            SetRelativeObject(element, "availabilityGroup", null);
            element.FindPropertyRelative("availableAlpha").floatValue = 1f;
            element.FindPropertyRelative("unavailableAlpha").floatValue = 0.35f;
        }
    }

    private static void AssignLobbySlots(Scene scene, SerializedObject serializedLobby)
    {
        EnsureLobbySlotInstances(scene);
        Transform[] slots = GetOrderedLobbySlots(scene);

        SerializedProperty slotViews = serializedLobby.FindProperty("lobbySlots");
        slotViews.arraySize = slots.Length;

        for (int i = 0; i < slots.Length; i++)
        {
            Transform slot = slots[i];
            Image frameImage = EnsureSlotFrame(slot);
            Outline frameOutline = frameImage != null ? frameImage.GetComponent<Outline>() : null;
            TMP_Text statusText = EnsureSlotStatusText(slot);
            Button addButton = slot.Find("AddBotButton")?.GetComponent<Button>();
            Button removeButton = slot.Find("RemoveBotButton")?.GetComponent<Button>();

            ClearPersistentButtonCalls(addButton);
            ClearPersistentButtonCalls(removeButton);
            EnsureButtonFeedback(addButton);
            EnsureButtonFeedback(removeButton);

            SerializedProperty element = slotViews.GetArrayElementAtIndex(i);
            SetRelativeObject(element, "root", slot as RectTransform);
            SetRelativeObject(element, "statusText", statusText);
            SetRelativeObject(element, "addBotButton", addButton);
            SetRelativeObject(element, "removeBotButton", removeButton);
            SetRelativeObject(element, "frameImage", frameImage);
            SetRelativeObject(element, "frameOutline", frameOutline);
        }
    }

    private static void AuthorMultiplayerConnectionScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject canvasObject = new("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(MultiplayerConnectionMenu));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        EnsureRenderingCamera();

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Button buttonPrefab = AssetDatabase.LoadAssetAtPath<Button>(ButtonPrefabPath);
        MultiplayerConnectionMenu menu = canvasObject.GetComponent<MultiplayerConnectionMenu>();

        GameObject connectionType = CreatePanel("ConnectionType", canvasObject.transform, Vector2.zero, new Vector2(380f, 230f));
        Image panelImage = connectionType.GetComponent<Image>() ?? connectionType.AddComponent<Image>();
        panelImage.color = new Color(0.015f, 0.018f, 0.02f, 0.88f);
        panelImage.raycastTarget = true;

        TMP_Text title = EnsureText("Title", connectionType.transform, "MULTIPLAYER", new Vector2(0f, 76f), new Vector2(340f, 44f), 34f);
        title.fontStyle = FontStyles.UpperCase;

        Button hostButton = EnsureButton("HostGameButton", connectionType.transform, buttonPrefab, "HOST GAME", new Vector2(0f, 14f), new Vector2(260f, 56f));
        Button joinButton = EnsureButton("JoinGameButton", connectionType.transform, buttonPrefab, "JOIN GAME", new Vector2(0f, -58f), new Vector2(260f, 56f));
        SetPersistentButtonCall(hostButton, menu.HostGame);
        SetPersistentButtonCall(joinButton, menu.JoinGame);

        AuthorMultiplayerMenu(scene, menu);
        EnsureEventSystem();

        EditorSceneManager.SaveScene(scene, MultiplayerConnectionScenePath);
    }

    private static void UpdateMainMenuScene()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/_Project/_Scenes/MainMenu.unity");
        foreach (Button button in FindSceneComponents<Button>(scene))
        {
            SerializedObject serializedButton = new(button);
            SerializedProperty calls = serializedButton.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            bool changed = false;

            for (int i = 0; calls != null && i < calls.arraySize; i++)
            {
                SerializedProperty call = calls.GetArrayElementAtIndex(i);
                SerializedProperty stringArgument = call.FindPropertyRelative("m_Arguments.m_StringArgument");
                if (stringArgument == null || stringArgument.stringValue != MultiplayerLobbySceneName)
                    continue;

                stringArgument.stringValue = MultiplayerConnectionSceneName;
                changed = true;
            }

            if (changed)
                serializedButton.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureLobbySlotInstances(Scene scene)
    {
        Transform[] slots = GetOrderedLobbySlots(scene);
        if (slots.Length == 0)
            return;

        Transform parent = slots[0].parent;
        GameObject slotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerSlotPrefabPath);
        if (parent == null || slotPrefab == null)
            return;

        while (slots.Length < LobbySlotCount)
        {
            GameObject slotObject = (GameObject)PrefabUtility.InstantiatePrefab(slotPrefab, scene);
            slotObject.transform.SetParent(parent, false);
            slots = GetOrderedLobbySlots(scene);
        }

        slots = GetOrderedLobbySlots(scene);
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].name = i == 0 ? "PlayerSlot" : $"PlayerSlot_{i}";
            PositionLobbySlot(slots[i] as RectTransform, i);
        }

        if (parent is RectTransform parentRect)
            parentRect.sizeDelta = new Vector2(Mathf.Max(parentRect.sizeDelta.x, 140f), Mathf.Max(parentRect.sizeDelta.y, 230f));
    }

    private static Transform[] GetOrderedLobbySlots(Scene scene)
    {
        return FindSceneComponents<SwitchState>(scene)
            .Select(switchState => switchState.transform)
            .Where(transform => transform.name.StartsWith("PlayerSlot"))
            .OrderBy(GetSlotSortIndex)
            .ToArray();
    }

    private static void PositionLobbySlot(RectTransform slot, int index)
    {
        if (slot == null)
            return;

        slot.anchorMin = new Vector2(0.5f, 1f);
        slot.anchorMax = new Vector2(0.5f, 1f);
        slot.pivot = new Vector2(0.5f, 0.5f);
        slot.sizeDelta = new Vector2(48f, 48f);
        slot.anchoredPosition = new Vector2(index % 2 == 0 ? -35f : 35f, -55f - 55f * (index / 2));
        slot.localScale = Vector3.one;
    }

    private static GameObject CreatePanel(string objectName, Transform parent, Vector2 position, Vector2 size)
    {
        GameObject panel = new(objectName, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        ConfigureRect(panel.GetComponent<RectTransform>(), position, size);
        return panel;
    }

    private static TMP_InputField EnsureInput(string objectName, Transform parent, string text, Vector2 position, Vector2 size)
    {
        GameObject inputObject = FindDirectChild(parent, objectName)?.gameObject;
        if (inputObject == null)
            inputObject = CreatePanel(objectName, parent, position, size);

        ConfigureRect(inputObject.GetComponent<RectTransform>(), position, size);

        Image image = inputObject.GetComponent<Image>() ?? inputObject.AddComponent<Image>();
        image.color = new Color(0f, 0.996f, 0.925f, 0.22f);

        TMP_InputField input = inputObject.GetComponent<TMP_InputField>() ?? inputObject.AddComponent<TMP_InputField>();
        TMP_Text textComponent = EnsureText("Text", inputObject.transform, text, Vector2.zero, new Vector2(size.x - 24f, size.y), 22f);
        TMP_Text placeholder = EnsureText("Placeholder", inputObject.transform, "HOST ADDRESS", Vector2.zero, new Vector2(size.x - 24f, size.y), 18f);
        placeholder.color = new Color(1f, 1f, 1f, 0.42f);

        input.textComponent = textComponent;
        input.placeholder = placeholder;
        input.text = text;
        input.caretColor = Color.white;
        input.selectionColor = new Color(0f, 0.996f, 0.925f, 0.35f);
        return input;
    }

    private static Button EnsureButton(string objectName, Transform parent, Button prefab, string text, Vector2 position, Vector2 size)
    {
        GameObject buttonObject = FindDirectChild(parent, objectName)?.gameObject;
        if (buttonObject == null)
        {
            buttonObject = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject)
                : new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.name = objectName;
            buttonObject.transform.SetParent(parent, false);
        }

        ConfigureRect(buttonObject.GetComponent<RectTransform>(), position, size);
        Button button = buttonObject.GetComponent<Button>() ?? buttonObject.AddComponent<Button>();
        TMP_Text label = buttonObject.GetComponentInChildren<TMP_Text>(true) ??
            EnsureText("Label", buttonObject.transform, text, Vector2.zero, size, 24f);
        label.text = text;
        label.fontStyle = FontStyles.UpperCase;
        label.raycastTarget = false;

        EnsureButtonFeedback(button);
        return button;
    }

    private static TMP_Text EnsureText(string objectName, Transform parent, string text, Vector2 position, Vector2 size, float fontSize)
    {
        GameObject textObject = FindDirectChild(parent, objectName)?.gameObject;
        if (textObject == null)
            textObject = CreatePanel(objectName, parent, position, size);

        ConfigureRect(textObject.GetComponent<RectTransform>(), position, size);
        TMP_Text label = textObject.GetComponent<TMP_Text>() ?? textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static GameObject EnsureTrailColorFrame(Transform button)
    {
        Transform existing = FindDirectChild(button, "SelectionFrame");
        GameObject frame = existing != null ? existing.gameObject : new GameObject("SelectionFrame", typeof(RectTransform));
        frame.transform.SetParent(button, false);
        frame.transform.SetAsFirstSibling();
        frame.SetActive(false);

        RectTransform rect = frame.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(-4f, -4f);
        rect.offsetMax = new Vector2(4f, 4f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        Image image = frame.GetComponent<Image>() ?? frame.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.02f);
        image.raycastTarget = false;

        Outline outline = frame.GetComponent<Outline>() ?? frame.AddComponent<Outline>();
        outline.effectColor = Color.white;
        outline.effectDistance = new Vector2(2f, -2f);
        return frame;
    }

    private static Image EnsureSlotFrame(Transform slot)
    {
        Transform existing = FindDirectChild(slot, "SlotFrame");
        GameObject frame = existing != null ? existing.gameObject : new GameObject("SlotFrame", typeof(RectTransform));
        frame.transform.SetParent(slot, false);
        frame.transform.SetAsFirstSibling();

        RectTransform rect = frame.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(3f, 3f);
        rect.offsetMax = new Vector2(-3f, -3f);

        Image image = frame.GetComponent<Image>() ?? frame.AddComponent<Image>();
        image.color = new Color(0f, 0.996f, 0.925f, 0.07f);
        image.raycastTarget = false;

        Outline outline = frame.GetComponent<Outline>() ?? frame.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.996f, 0.925f, 0.65f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return image;
    }

    private static TMP_Text EnsureSlotStatusText(Transform slot)
    {
        TMP_Text label = FindDirectChild(slot, "SlotStatusText")?.GetComponent<TMP_Text>();
        if (label == null)
            label = EnsureText("SlotStatusText", slot, string.Empty, Vector2.zero, new Vector2(112f, 52f), 14f);

        label.fontStyle = FontStyles.UpperCase;
        label.color = new Color(0f, 0.996f, 0.925f, 1f);
        label.raycastTarget = false;
        return label;
    }

    private static void EnsureButtonFeedback(Button button)
    {
        if (button == null)
            return;

        Button prefab = AssetDatabase.LoadAssetAtPath<Button>(ButtonPrefabPath);
        ButtonScript prefabFeedback = prefab != null ? prefab.GetComponent<ButtonScript>() : null;

        if (!button.TryGetComponent(out ButtonScript feedback))
            feedback = button.gameObject.AddComponent<ButtonScript>();

        feedback.ApplyStyleFrom(prefabFeedback);
    }

    private static void SetPersistentButtonCall(Button button, UnityAction action)
    {
        if (button == null || action == null)
            return;

        ClearPersistentButtonCalls(button);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void ClearPersistentButtonCalls(Button button)
    {
        if (button == null)
            return;

        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(button.onClick, 0);

        EditorUtility.SetDirty(button);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static void EnsureRenderingCamera()
    {
        if (Object.FindFirstObjectByType<Camera>() != null)
            return;

        GameObject cameraObject = new("Main Camera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.cullingMask = 0;
        camera.depth = -100f;
    }

    private static void EnsureSceneInBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        EditorBuildSettingsScene existing = scenes.FirstOrDefault(scene => scene.path == scenePath);
        if (existing != null)
        {
            existing.enabled = true;
            EditorBuildSettings.scenes = scenes;
            return;
        }

        EditorBuildSettings.scenes = scenes
            .Concat(new[] { new EditorBuildSettingsScene(scenePath, true) })
            .ToArray();
    }

    private static void RemoveButtonEventTriggers(Scene scene)
    {
        foreach (EventTrigger trigger in FindSceneComponents<EventTrigger>(scene))
        {
            if (trigger != null && trigger.GetComponent<ButtonScript>() != null)
                Object.DestroyImmediate(trigger, true);
        }
    }

    private static void ConfigureRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    private static int GetSlotSortIndex(Transform slot)
    {
        if (slot == null)
            return int.MaxValue;

        if (slot.name == "PlayerSlot")
            return 0;

        int underscore = slot.name.LastIndexOf('_');
        return underscore >= 0 && int.TryParse(slot.name[(underscore + 1)..], out int index)
            ? index
            : int.MaxValue;
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    private static GameObject FindSceneGameObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindDeepChild(root.transform, objectName);
            if (match != null)
                return match.gameObject;
        }

        return null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent.name == childName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform match = FindDeepChild(parent.GetChild(i), childName);
            if (match != null)
                return match;
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        return FindSceneComponents<T>(scene).FirstOrDefault();
    }

    private static T[] FindSceneComponents<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .ToArray();
    }

    private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }

    private static void SetString(SerializedObject serializedObject, string propertyName, string value)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            property.stringValue = value;
    }

    private static void SetRelativeObject(SerializedProperty parent, string propertyName, Object value)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property != null)
            property.objectReferenceValue = value;
    }
}
#endif
