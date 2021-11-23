namespace Game
{
    public class SyncListClothing : Mirror.SyncList<WardrobeItem>
    {
        public void Initiate()
        {
            for (int i = 0; i < Storage.data.wardrobe.count; i++)
            {
                objects.Add(new WardrobeItem());
            }
        }
        public int GetIndex(ushort id)
        {
            if(Count > 0)
            {
                for(int i = 0; i < Count; i++)
                {
                    if(objects[i].id == id)
                    {
                        return i;
                    }
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
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}