using UnityEngine;

public sealed class PersistentUiAudioPlayer : MonoBehaviour
{
    private static PersistentUiAudioPlayer _instance;

    private AudioSource _audioSource;

    public static void PlayOneShot(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        EnsureInstance();
        _instance._audioSource.PlayOneShot(clip, volumeScale);
    }

    private static void EnsureInstance()
    {
        if (_instance != null)
            return;

        GameObject go = new(nameof(PersistentUiAudioPlayer));
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<PersistentUiAudioPlayer>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
        _audioSource.ignoreListenerPause = true;
    }
}
