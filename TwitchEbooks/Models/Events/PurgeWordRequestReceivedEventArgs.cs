namespace TwitchEbooks.Models.Events
{
    public class PurgeWordRequestReceivedEventArgs
    {
        public uint ChannelId { get; set; }
        public string Word { get; set; }
    }
}
