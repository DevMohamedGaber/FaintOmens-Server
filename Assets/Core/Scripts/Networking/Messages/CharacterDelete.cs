namespace Game.Network.Messages
{
    public struct CharacterDelete : Mirror.NetworkMessage
    {
        public uint id;
    }
}