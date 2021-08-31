namespace Game.DatabaseModels
{
    public class Mails
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public uint? id { get; set; } = null;
        public uint recieverId { get; set; }
        public MailCategory category { get; set; }
        //public int sender { get; set; }
        public string subject { get; set; }
        public string content { get; set; }
        public bool opened { get; set; }
        public uint gold { get; set; }
        public uint diamonds { get; set; }
        public uint b_diamonds { get; set; }
        public bool currencyRecieved { get; set; }
        public double send_time { get; set; }
    }
}