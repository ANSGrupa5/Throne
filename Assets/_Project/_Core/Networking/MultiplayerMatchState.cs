public static class MultiplayerMatchState
{
    public static bool IsFrozen { get; private set; }

    public static void SetFrozen(bool frozen)
    {
        IsFrozen = frozen;
    }
}
