namespace TwitchEbooks.Models.EventSub
{
    public record Transport()
    {
        public string Method { get; init; }
        public string Callback { get; init; }
        public string Secret { get; init; }
    }

    public record SubscriptionRequest()
    {
        public string Type { get; init; }
        public string Version { get; init; } = "1";
        public object Condition { get; init; }
        public Transport Transport { get; init; }
    }
}
