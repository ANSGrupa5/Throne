using UnityEngine;

public sealed class LobbyRootSelector : MonoBehaviour
{
    [SerializeField] private LobbyMode fallbackMode = LobbyMode.Singleplayer;
    [SerializeField] private GameObject singleplayerRoot;
    [SerializeField] private GameObject multiplayerRoot;

    private void Awake()
    {
        ApplySelection();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (singleplayerRoot == multiplayerRoot && singleplayerRoot != null)
            Debug.LogWarning($"{nameof(LobbyRootSelector)} on {name} uses the same object for both lobby roots.", this);
    }
#endif

    public void ApplySelection()
    {
        bool useMultiplayer = ShouldUseMultiplayerRoot();

        if (singleplayerRoot != null)
            singleplayerRoot.SetActive(!useMultiplayer);
        if (multiplayerRoot != null)
            multiplayerRoot.SetActive(useMultiplayer);

        ValidateActiveControllers();
    }

    private bool ShouldUseMultiplayerRoot()
    {
        MultiplayerRuntimeBootstrap bootstrap = MultiplayerRuntimeBootstrap.Instance;
        if (bootstrap != null && (bootstrap.IsServerStarted || bootstrap.IsClientStarted))
            return true;

        return LobbyLaunchContext.ResolveMode(fallbackMode) != LobbyMode.Singleplayer;
    }

    private void ValidateActiveControllers()
    {
        int activeControllerCount = 0;
        LobbyController[] controllers = FindObjectsByType<LobbyController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] != null && controllers[i].isActiveAndEnabled)
                activeControllerCount++;
        }

        if (activeControllerCount != 1)
        {
            Debug.LogWarning(
                $"{nameof(LobbyRootSelector)} expected exactly one active {nameof(LobbyController)}, but found {activeControllerCount}.",
                this);
        }
    }
}
