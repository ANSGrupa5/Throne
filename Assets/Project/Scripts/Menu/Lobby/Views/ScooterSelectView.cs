using System;
using UnityEngine;

[Serializable]
public sealed class ScooterSelectView : LobbyComponent
{
    [Header("Selection")]
    [SerializeField] private int currentModel;

    [Header("Vehicle Previews")]
    [SerializeField] private Transform previewRoot;
    [SerializeField] private GameObject[] motorPreview;
    [SerializeField] private GameObject[] motorPlayable;

    private GameObject _activePreview;
    private int _activePreviewModel = -1;

    public int SelectedModelIndex => currentModel;

    public void Validate(LobbyController owner)
    {
        string ownerName = owner != null ? owner.name : nameof(LobbyController);

        if (previewRoot == null)
            Debug.LogWarning($"{nameof(LobbyController)} on {ownerName} has no scooter preview root assigned.", owner);

        if ((motorPreview == null || motorPreview.Length == 0) &&
            (motorPlayable == null || motorPlayable.Length == 0))
        {
            Debug.LogError($"{nameof(LobbyController)} on {ownerName} has no scooter preview or playable prefabs assigned.", owner);
        }

        if (motorPlayable == null || motorPlayable.Length == 0)
            Debug.LogError($"{nameof(LobbyController)} on {ownerName} has no playable scooter prefabs assigned.", owner);

        if (motorPreview != null &&
            motorPlayable != null &&
            motorPreview.Length > 0 &&
            motorPlayable.Length > 0 &&
            motorPreview.Length != motorPlayable.Length)
        {
            Debug.LogError($"{nameof(LobbyController)} on {ownerName} has mismatched scooter preview/playable counts.", owner);
        }
    }

    protected override void OnInitialize()
    {
        if (currentModel < 0)
            currentModel = 0;

        SetPlayerModel(currentModel);
    }

    public override void Refresh()
    {
        SetPlayerModel(currentModel);
    }

    public void ChangePlayerModelUp()
    {
        int optionCount = GetModelOptionCount();
        if (optionCount == 0)
            return;

        int model = currentModel + 1;
        if (model >= optionCount)
            model = 0;

        SetPlayerModel(model);
    }

    public void ChangePlayerModelDown()
    {
        int optionCount = GetModelOptionCount();
        if (optionCount == 0)
            return;

        int model = currentModel - 1;
        if (model < 0)
            model = optionCount - 1;

        SetPlayerModel(model);
    }

    public void SetPlayerModel(int selectedMotor)
    {
        int optionCount = GetModelOptionCount();
        if (optionCount == 0)
            return;

        int previousModel = currentModel;
        int model = Mathf.Clamp(selectedMotor, 0, optionCount - 1);
        currentModel = model;

        RefreshPreviewInstance(model);

        if (Lobby.PlayerLook != null &&
            motorPlayable != null &&
            model >= 0 &&
            model < motorPlayable.Length &&
            motorPlayable[model] != null)
        {
            Lobby.PlayerLook.playerPrefab = motorPlayable[model];
        }

        if (model != previousModel)
            Lobby.MarkLobbyStateDirty();
    }

    private int GetModelOptionCount()
    {
        int previewCount = motorPreview != null ? motorPreview.Length : 0;
        int playableCount = motorPlayable != null ? motorPlayable.Length : 0;
        return Mathf.Max(previewCount, playableCount);
    }

    private void RefreshPreviewInstance(int model)
    {
        if (previewRoot == null)
            return;

        if (_activePreview != null && _activePreviewModel == model)
            return;

        ClearPreviewInstance();

        GameObject previewPrefab = ResolvePreviewPrefab(model);
        if (previewPrefab == null)
            return;

        _activePreview = UnityEngine.Object.Instantiate(previewPrefab, previewRoot);
        _activePreview.name = previewPrefab.name;
        _activePreview.transform.localPosition = Vector3.zero;
        _activePreview.transform.localRotation = Quaternion.identity;
        _activePreview.transform.localScale = Vector3.one;
        _activePreviewModel = model;
    }

    private GameObject ResolvePreviewPrefab(int model)
    {
        if (motorPreview != null &&
            model >= 0 &&
            model < motorPreview.Length &&
            motorPreview[model] != null)
        {
            return motorPreview[model];
        }

        if (motorPlayable != null &&
            model >= 0 &&
            model < motorPlayable.Length)
        {
            return motorPlayable[model];
        }

        return null;
    }

    private void ClearPreviewInstance()
    {
        if (_activePreview == null)
            return;

        UnityEngine.Object.Destroy(_activePreview);
        _activePreview = null;
        _activePreviewModel = -1;
    }
}
