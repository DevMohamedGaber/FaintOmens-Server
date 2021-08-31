namespace Game
{
    [System.Serializable]
    public struct Trade {
        public TradeOffer player1;
        public TradeOffer player2;
        //public bool IsBothConfirmed => player1.confirmed && player2.confirmed;
        public bool IsBothAccepted => player1.accepted && player2.accepted;
        public Trade(uint p1Id, uint p2Id)
        {
            player1 = new TradeOffer(p1Id);
            player2 = new TradeOffer(p2Id);
        }
        public void Confirm(uint pId, TradeOfferContent content)
        {
            if(player1.id == pId) player1.content = content;
            else if(player2.id == pId) player2.content = content;
        }
        public void Accept(uint pId, bool accepted = true)
        {
            if(player1.id == pId) player1.accepted = accepted;
            else if(player2.id == pId) player2.accepted = accepted;
        }
        public bool ValidateOffer(Player player)
        {
            if(player1.id == player.id)
                return player1.content.IsValid(player);
            else if(player2.id == player.id)
                return player2.content.IsValid(player);
            return false;
        }
        public TradeOfferContent GetOfferContent(uint pId)
        {
            if(player1.id == pId) return player1.content;
            else if(player2.id == pId) return player2.content;
            return TradeOfferContent.Empty;
        }
        public bool IsAccepted(uint pId)
        {
            if(player1.id == pId)
                return player1.accepted;
            if(player2.id == pId)
                return player2.accepted;
            return false;
        }
        public bool IsPartOfTrade(uint id)
        {
            return player1.id == id || player2.id == id;
        }
        /*public bool IsConfirmed(uint id) {
            if(player1.id == id)
                return player1.confirmed;
            else if(player2.id == id)
                return player2.confirmed;
            return false;
        }*/
        public uint GetOtherPlayer(uint pId)
        {
            return player1.id == pId ? player2.id : player1.id;
        }
    }
}