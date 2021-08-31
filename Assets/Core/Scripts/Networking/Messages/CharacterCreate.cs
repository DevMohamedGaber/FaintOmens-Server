namespace Game.Network.Messages
{
    public struct CharacterCreate : Mirror.NetworkMessage
    {
        public string name;
        public PlayerClass classId;
        public Gender gender;
        public byte tribeId;
    }
}