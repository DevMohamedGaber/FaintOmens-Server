namespace Game.Achievements
{
    [System.Serializable]
    public struct Archive
    {
        public ushort achievementPoints;
        public ulong gainedGold;
        public ulong usedGold;
        public uint gainedDiamonds;
        public uint usedDiamonds;
        public uint gainedBDiamonds;
        public uint usedBDiamonds;
        public ushort killStrike;
        public ushort arena1v1Wins;
        public ushort arena1v1Losses;
        public ushort highestArena1v1Points;
    }
}