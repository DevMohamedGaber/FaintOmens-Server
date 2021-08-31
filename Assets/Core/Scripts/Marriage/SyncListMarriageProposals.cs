namespace Game
{
    public class SyncListMarriageProposals : Mirror.SyncList<MarriageProposal>
    {
        public bool Has(uint id)
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