using System;
using System.Text.Json;

namespace TwitchEbooks.Twitch.Extensions
{
    public static class JsonExtensions
    {
        public static JsonDocument SerializeToDocument<TValue>(TValue value, JsonSerializerOptions options = default)
           => SerializeToDocument(value, typeof(TValue), options);

        public static JsonDocument SerializeToDocument(object value, Type type, JsonSerializerOptions options = default)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value, options);
            return JsonDocument.Parse(bytes);
        }

        public static JsonElement SerializeToElement<TValue>(TValue value, JsonSerializerOptions options = default)
            => SerializeToElement(value, typeof(TValue), options);

        public static JsonElement SerializeToElement(object value, Type type, JsonSerializerOptions options = default)
        {
            using var doc = SerializeToDocument(value, type, options);
            return doc.RootElement.Clone();
        }
    }
}
