using UnityEngine;

public sealed class ScooterSelectView : LobbyComponent
{
    protected override void OnInitialize()
    {
        if (Lobby.CurrentModel < 0)
            Lobby.CurrentModel = 0;

        SetPlayerModel(Lobby.CurrentModel);
    }

    public override void Refresh()
    {
        SetPlayerModel(Lobby.CurrentModel);
    }

    public void ChangePlayerModelUp()
    {
        GameObject[] previews = Lobby.MotorPreview;
        if (previews == null || previews.Length == 0)
            return;

        int model = Lobby.CurrentModel + 1;
        if (model >= previews.Length)
            model = 0;

        SetPlayerModel(model);
    }

    public void ChangePlayerModelDown()
    {
        GameObject[] previews = Lobby.MotorPreview;
        if (previews == null || previews.Length == 0)
            return;

        int model = Lobby.CurrentModel - 1;
        if (model < 0)
            model = previews.Length - 1;

        SetPlayerModel(model);
    }

    public void SetPlayerModel(int selectedMotor)
    {
        GameObject[] previews = Lobby.MotorPreview;
        if (previews == null || previews.Length == 0)
            return;

        int model = Mathf.Clamp(selectedMotor, 0, previews.Length - 1);
        Lobby.CurrentModel = model;

        for (int i = 0; i < previews.Length; i++)
        {
            if (previews[i] != null)
                previews[i].SetActive(i == model);
        }

        GameObject[] playable = Lobby.MotorPlayable;
        if (Lobby.PlayerLook != null && playable != null && model >= 0 && model < playable.Length && playable[model] != null)
            Lobby.PlayerLook.playerPrefab = playable[model];
    }
}
