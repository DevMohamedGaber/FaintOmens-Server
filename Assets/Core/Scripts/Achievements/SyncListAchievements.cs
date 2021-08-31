namespace Game
{
    public class SyncListAchievements : Mirror.SyncList<Achievements.Achievement>
    {
        public bool Has(ushort id)
        {
            if(Count > 0)
            {
                for(int i = 0; i < Count; i++)
                {
                    if(objects[i].id == id)
                        return true;
                }
            }
            return false;
        }
    }
}