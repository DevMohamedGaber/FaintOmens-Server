namespace Game.Network.Messages
{
    public struct Login : Mirror.NetworkMessage
    {
        public string account;
        public string password;
    }
}