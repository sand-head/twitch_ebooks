using Serilog.Core;
using Serilog.Events;
using System;
using System.Linq;
using System.Text.Json;

namespace TwitchEbooks.Infrastructure
{
    public class JsonElementDestructuringPolicy : IDestructuringPolicy
    {
        public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
        {
            if (value is not JsonElement element)
            {
                result = null;
                return false;
            }

            result = Destructure(element);
            return true;
        }

        private LogEventPropertyValue Destructure(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Undefined or JsonValueKind.Null => new ScalarValue(null),
                JsonValueKind.Object => new StructureValue(element.EnumerateObject().Select(p => new LogEventProperty(p.Name, Destructure(p.Value)))),
                JsonValueKind.Array => new SequenceValue(element.EnumerateArray().Select(i => Destructure(i))),
                JsonValueKind.String => new ScalarValue(element.GetString()),
                JsonValueKind.Number => new ScalarValue(element.GetDecimal()),
                JsonValueKind.True => new ScalarValue(true),
                JsonValueKind.False => new ScalarValue(false),
                _ => throw new ArgumentException($"JsonElement was of unrecognized kind {element.ValueKind}.")
            };
        }
    }
}
