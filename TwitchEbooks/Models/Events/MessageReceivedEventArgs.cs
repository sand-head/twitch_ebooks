using Anybotty.StreamClientLibrary.Common.Models.Messages;
using System;

namespace TwitchEbooks.Models.Events
{
    public class MessageReceivedEventArgs<TMessage> : EventArgs where TMessage : StreamMessage
    {
        public TMessage Message { get; set; }
    }
}
