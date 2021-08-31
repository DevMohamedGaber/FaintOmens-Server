namespace Game
{
    public class SyncListFriend : Mirror.SyncList<Friend>
    {
        public byte GetFriendLevel(uint id)
        {
            if(objects.Count > 0)
            {
                for(int i = 0; i < objects.Count; i++)
                {
                    if(objects[i].id == id)
                        return objects[i].friendship;
                }
            }
            return 0;
        }
    }
}