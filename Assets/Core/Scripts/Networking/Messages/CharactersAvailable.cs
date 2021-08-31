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
            public Gender gender;
            public byte level;
            public byte tribeId;
            public byte avatar;
            public bool showWardrobe;
            public EquipmentPreview[] equipment;
            public ushort[] wardrobe;
        }
    }
}