using System;

namespace TwitchEbooks.Models.EventSub
{
    public class SubscriptionList
    {
        public Datum[] Data { get; set; }
        public int Total { get; set; }
        public int Total_cost { get; set; }
        public int Max_total_cost { get; set; }
        public Pagination Pagination { get; set; }
    }

    public class Pagination
    {
    }

    public class Datum
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }
        public int Cost { get; set; }
        public Condition Condition { get; set; }
        public DateTime Created_at { get; set; }
        public Transport Transport { get; set; }
    }

    public class Condition
    {
        public string Broadcaster_user_id { get; set; }
    }
}
