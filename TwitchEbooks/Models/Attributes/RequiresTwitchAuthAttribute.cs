using System;

namespace TwitchEbooks.Models.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class RequiresTwitchAuthAttribute : Attribute
    {
    }
}
