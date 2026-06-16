using UnityEngine;

[CreateAssetMenu(menuName = "Throne/UI/Game Over Navigation Config")]
public sealed class GameOverNavigationConfig : ScriptableObject
{
    [SerializeField] private SceneReference mainMenuScene;
    [SerializeField] private SceneReference singleplayerLobbyScene;

    public string MainMenuSceneName => mainMenuScene != null ? mainMenuScene.SceneName : string.Empty;
    public string SingleplayerLobbySceneName => singleplayerLobbyScene != null ? singleplayerLobbyScene.SceneName : string.Empty;

#if UNITY_EDITOR
    private void OnValidate()
    {
        mainMenuScene?.SyncSceneNameFromAsset();
        singleplayerLobbyScene?.SyncSceneNameFromAsset();
    }
#endif
}
