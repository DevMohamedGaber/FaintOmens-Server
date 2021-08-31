namespace Game.DatabaseModels
{
    public class PreviewData
    {
        [SQLite.PrimaryKey]
        public uint id { get; set; }
        public int health { get; set; }
        public int mana { get; set; }
        public int pAtk { get; set; }
        public int mAtk { get; set; }
        public int pDef { get; set; }
        public int mDef { get; set; }
        public float block { get; set; }
        public float untiBlock { get; set; }
        public float critRate { get; set; }
        public float critDmg { get; set; }
        public float antiCrit { get; set; }
        public float untiStun { get; set; }
        public float speed { get; set; }
        public System.DateTime lastSave { get; set; }
    }
}