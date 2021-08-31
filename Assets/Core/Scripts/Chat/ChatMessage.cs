using System;
namespace Game
{
    [Serializable]
    public struct ChatMessage {
        public ChatSenderInfo sender;
        public ChatChannels channel;
        public string message;
        public double sendTime;
        public ChatMessage(ChatSenderInfo sender, ChatChannels channel, string message) {
            this.sender = sender;
            this.channel = channel;
            this.message = message;
            this.sendTime = DateTime.Now.ToOADate();
        }
    }
}