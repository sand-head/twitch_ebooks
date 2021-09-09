using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TwitchEbooks.Models.EventSub
{
    public class EventSubPayload
    {
        public Subscription Subscription { get; set; }
        public Event Event { get; set; }
    }

    public class Subscription
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public string Version { get; set; }
        public string Status { get; set; }
        public int Cost { get; set; }
        public Condition Condition { get; set; }
        public Transport Transport { get; set; }
        public DateTime Created_at { get; set; }
    }

    public class Event
    {
        public bool Is_anonymous { get; set; }
        public string User_id { get; set; }
        public string User_login { get; set; }
        public string User_name { get; set; }
        public string Broadcaster_user_id { get; set; }
        public string Broadcaster_user_login { get; set; }
        public string Broadcaster_user_name { get; set; }
        public string Message { get; set; }
        public int Bits { get; set; }
    }
}
