using UnityEngine;

public class GameSessionBootstrap : MonoBehaviour
{
    public static GameSessionRuntime CurrentSession { get; private set; }

    private static GameSessionBootstrap _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void SetSession(GameSessionRuntime session)
    {
        EnsureInstance();
        CurrentSession = session;
    }

    public static bool TryGetSession(out GameSessionRuntime session)
    {
        session = CurrentSession;
        return session != null;
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
            return;

        var bootstrapObject = new GameObject(nameof(GameSessionBootstrap));
        _instance = bootstrapObject.AddComponent<GameSessionBootstrap>();
    }
}
