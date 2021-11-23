namespace Game.Network.Messages
{
    public struct CharactersAvailable : Mirror.NetworkMessage
    {
        public CharacterPreview[] characters;
        
        public struct CharacterPreview
        {
            public uint id;
            public string name;
            public PlayerClassData classInfo;
            public byte level;
            public byte tribeId;
            public byte avatar;
            public PlayerModelData model;
        }
    }
}