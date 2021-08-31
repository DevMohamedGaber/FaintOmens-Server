namespace Game.Arena
{
    [System.Serializable]
    public enum ArenaMatchState : byte
    {
        Notified,
        WaitingPlayersToTeleport,
        CountingDown,
        Started,
        Finished
    }
}