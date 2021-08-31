namespace Game.Network.Messages
{
    public struct LoginSuccess : Mirror.NetworkMessage
    {
        public byte[] tribes;
        public string timeZoneId;
        public LoginSuccess(byte[] tribes, string timeZoneId)
        {
            this.tribes = tribes;
            this.timeZoneId = timeZoneId;
        }
    }
}