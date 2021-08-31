namespace Game.Network
{
    [System.Serializable]
    public enum NetworkState
    {
        Offline,
        Handshake,
        Lobby,
        World
    }
}