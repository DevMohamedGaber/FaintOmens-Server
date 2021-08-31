using System.Collections.Generic;
namespace Game.Achievements
{
    [System.Serializable]
    public struct Achievement
    {
        public ushort id;
        public bool claimed;
        public AchievementTypes type => data.type;
        public ScriptableAchievement successor => data.successor;
        public ScriptableAchievement data
        {
            get
            {
                if(!ScriptableAchievement.dict.ContainsKey(id))
                {
                    throw new KeyNotFoundException("There is no ScriptableAchievement with ID=" + id + ". Make sure that all ScriptableAchievement are in the Resources folder so they are loaded properly.");
                }
                return ScriptableAchievement.dict[id];
            }
        }
        public Achievement(ushort id)
        {
            this.id = id;
            this.claimed = false;
        }
    }
}