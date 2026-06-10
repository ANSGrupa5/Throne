using System;
using UnityEngine;

[Serializable]
public sealed class ScooterSelectView : LobbyComponent
{
    [Header("Selection")]
    [SerializeField] private int currentModel;

    [Header("Vehicle Previews")]
    [SerializeField] private GameObject[] motorPreview;
    [SerializeField] private GameObject[] motorPlayable;

    public int SelectedModelIndex => currentModel;

    public void Validate(LobbyController owner)
    {
        string ownerName = owner != null ? owner.name : nameof(LobbyController);

        if (motorPreview == null || motorPreview.Length == 0)
            Debug.LogError($"{nameof(LobbyController)} on {ownerName} has no scooter preview objects assigned.", owner);

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
        if (motorPreview == null || motorPreview.Length == 0)
            return;

        int model = currentModel + 1;
        if (model >= motorPreview.Length)
            model = 0;

        SetPlayerModel(model);
    }

    public void ChangePlayerModelDown()
    {
        if (motorPreview == null || motorPreview.Length == 0)
            return;

        int model = currentModel - 1;
        if (model < 0)
            model = motorPreview.Length - 1;

        SetPlayerModel(model);
    }

    public void SetPlayerModel(int selectedMotor)
    {
        if (motorPreview == null || motorPreview.Length == 0)
            return;

        int previousModel = currentModel;
        int model = Mathf.Clamp(selectedMotor, 0, motorPreview.Length - 1);
        currentModel = model;

        for (int i = 0; i < motorPreview.Length; i++)
        {
            if (motorPreview[i] != null)
                motorPreview[i].SetActive(i == model);
        }

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
}
