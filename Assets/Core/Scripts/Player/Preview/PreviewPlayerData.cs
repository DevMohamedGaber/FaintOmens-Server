namespace Game
{
    [System.Serializable]
    public struct PreviewPlayerData
    {
        public bool status;
        // info
        public uint id;
        public string name;
        public byte level;
        public uint br;
        public Gender gender;
        public PlayerClassData classInfo;
        public byte tribeId;
        public string guildName;
        public byte vipLevel;
        public byte militaryRank;
        // attributes
        public int health;
        public int mana;
        public int pAtk;
        public int mAtk;
        public int pDef;
        public int mDef;
        public float block;
        public float untiBlock;
        public float critRate;
        public float critDmg;
        public float antiCrit;
        public float untiStun;
        public float speed;
        public Item[] equipments;
        public Item[] accessories;
    }
}