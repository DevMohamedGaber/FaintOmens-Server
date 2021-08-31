namespace Game
{
    public class SyncListItemSlot : Mirror.SyncList<ItemSlot>
    {
        public void Initiate(int initCount)
        {
            if(initCount > 0)
            {
                for(int i = 0; i < initCount; i++)
                    objects.Add(new ItemSlot());
            }
        }
    }
}