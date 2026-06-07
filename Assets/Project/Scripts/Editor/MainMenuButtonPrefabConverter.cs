using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MainMenuButtonPrefabConverter
{
    private const string ScenePath = "Assets/Project/Scenes/UI/Menu/MainMenu.unity";
    private const string ButtonPrefabPath = "Assets/Project/Prefabs/UI/Menu/Button.prefab";

    [MenuItem("Throne/Tools/Convert Main Menu Buttons To Prefabs")]
    public static void Convert()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
        if (prefab == null)
            throw new System.InvalidOperationException($"Missing prefab at {ButtonPrefabPath}");

        Menu menu = Object.FindFirstObjectByType<Menu>();
        if (menu == null)
            throw new System.InvalidOperationException("MainMenu scene does not contain a Menu component.");

        Transform mainScreen = FindTransformByName("MainScreen");
        if (mainScreen == null)
            throw new System.InvalidOperationException("Could not find MainScreen in MainMenu scene.");

        ReplaceButton(mainScreen, prefab, "SingleplayerButton", "Singleplayer", button =>
            UnityEventTools.AddStringPersistentListener(button.onClick, menu.LoadScene, "SingleplayerLobby"));
        ReplaceButton(mainScreen, prefab, "MultiplayerButton", "Multiplayer", button =>
            UnityEventTools.AddStringPersistentListener(button.onClick, menu.LoadScene, "MultiplayerConnection"));
        ReplaceButton(mainScreen, prefab, "OptionsButton", "Options", button =>
            UnityEventTools.AddPersistentListener(button.onClick, menu.ShowOptions));
        ReplaceButton(mainScreen, prefab, "StatisticsButton", "Statistics", button =>
            UnityEventTools.AddPersistentListener(button.onClick, menu.ShowStatisticsScreen));
        ReplaceButton(mainScreen, prefab, "ExitButton", "Exit", button =>
            UnityEventTools.AddPersistentListener(button.onClick, menu.Exit));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ReplaceButton(
        Transform parent,
        GameObject prefab,
        string buttonName,
        string label,
        System.Action<Button> configureClick)
    {
        Transform existing = parent.Find(buttonName);
        if (existing == null)
            throw new System.InvalidOperationException($"Could not find {buttonName} under MainScreen.");

        RectTransform existingRect = existing as RectTransform;
        int siblingIndex = existing.GetSiblingIndex();

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = buttonName;
        instance.transform.SetSiblingIndex(siblingIndex);

        RectTransform instanceRect = instance.GetComponent<RectTransform>();
        CopyRectTransform(existingRect, instanceRect);

        TMP_Text text = instance.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
            text.text = label;

        Button button = instance.GetComponent<Button>();
        if (button == null)
            throw new System.InvalidOperationException($"{ButtonPrefabPath} does not contain a Button component.");

        button.onClick = new Button.ButtonClickedEvent();
        configureClick(button);

        Object.DestroyImmediate(existing.gameObject);
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance);
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
}
