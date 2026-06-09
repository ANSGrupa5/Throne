public interface IMatchStartFlow
{
    bool CanStart(LobbyState state);
    void StartMatch(LobbyState state);
}
