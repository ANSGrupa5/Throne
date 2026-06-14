using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public sealed class SceneReference
{
#if UNITY_EDITOR
    [SerializeField] private SceneAsset sceneAsset;
#endif

    [SerializeField] private string sceneName;

    public string SceneName => sceneName ?? string.Empty;

    public SceneReference Clone()
    {
        return new SceneReference
        {
#if UNITY_EDITOR
            sceneAsset = sceneAsset,
#endif
            sceneName = sceneName
        };
    }

#if UNITY_EDITOR
    public void SyncSceneNameFromAsset()
    {
        if (sceneAsset != null)
            sceneName = sceneAsset.name;
    }
#endif
}
