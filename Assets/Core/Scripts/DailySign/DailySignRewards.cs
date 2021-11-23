using System.Collections.Generic;
namespace Game
{
    [System.Serializable]
    public struct DailySignRewards
    {
        public int daysCount;
        public List<ItemSlot> rewards;
    }
}