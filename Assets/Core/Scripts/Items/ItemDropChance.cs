namespace Game
{
    [System.Serializable]
    public class ItemDropChance
    {
        public Item item;
        [UnityEngine.Range(0,1)] public float probability;
    }
}