using Anybotty.StreamClientLibrary.Common.Models.Messages;
using System;

namespace TwitchEbooks.Models.Events
{
    public class ChatMessageReceivedEventArgs : EventArgs
    {
        public ChatMessage Message { get; set; }
    }
}
