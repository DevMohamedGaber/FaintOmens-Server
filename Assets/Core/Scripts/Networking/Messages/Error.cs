namespace Game.Network.Messages
{
    public struct Error : Mirror.NetworkMessage {
        public NetworkError error;
        public NetworkErrorAction action;
    }
}