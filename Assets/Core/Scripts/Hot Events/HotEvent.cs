namespace Game
{
    [System.Serializable]
    public struct HotEvent
    {
        public int id;
        public string[] name;
        public HotEventTypes type;
        [UnityEngine.TextArea(1, 30)] public string[] description;
        public double startsAt;
        public double endsAt;
        public bool renewable;
        public HotEventObjective[] objectives;
    }
}




