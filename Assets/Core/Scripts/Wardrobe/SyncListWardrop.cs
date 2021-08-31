namespace Game
{
    public class SyncListWardrop : Mirror.SyncList<WardrobeItem>
    {
        public int GetIndex(ushort id)
        {
            if(Count > 0)
            {
                for(int i = 0; i < Count; i++)
                {
                    if(objects[i].id == id)
                        return i;
                }
            }
            return -1;
        }
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