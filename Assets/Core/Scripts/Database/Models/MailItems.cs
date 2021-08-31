namespace Game.DatabaseModels
{
    public class MailItems : BaseItem
    {
        public uint mailId { get; set; }
        public bool recieved { get; set; } = false;
    }
}