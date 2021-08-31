using System;
using System.Collections.Generic;
namespace Game
{
    [Serializable]
    public struct VIP
    {
        public byte level;
        public int points;
        public int quests;
        public int[] firstRewards;
        public bool weeklyReward;
        public int totalRecharge;
        public int todayRecharge;

        public ScriptableVIP data
        {
            get
            {
                return ScriptableVIP.dict.ContainsKey(level) ? ScriptableVIP.dict[level] : ScriptableVIP.dict[0];
            }
        }
        public string FirstRewardsString()
        {
            if(firstRewards.Length > 0)
            {
                string result = firstRewards[0].ToString();
                for (int i = 1; i < firstRewards.Length; i++)
                {
                    result += "," + firstRewards[i].ToString();
                }
                return result;
            }
            return "";
        }
        public bool FirstRewardClaimed(int level)
        {
            for (int i = 0; i < firstRewards.Length; i++)
            {
                if(i == level)
                    return true;
            }
            return false;
        }
    }
}