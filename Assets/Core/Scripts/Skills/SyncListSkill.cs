namespace Game
{
    public class SyncListSkill : Mirror.SyncList<Skill>
    {
        public int IndexOf(ushort id) {
            for(int i = 0; i < objects.Count; i++)
            {
                if(objects[i].id == id)
                    return i;
            }
            return -1;
        }
    }
}