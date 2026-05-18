using Amazon.DynamoDBv2.Model;

namespace DynamoAwsManager
{
    /// <summary>
    /// Represents one DynamoDB row as a flat string dictionary so WPF DataGrid
    /// can bind to it via DynamicItem columns.
    /// </summary>
    public class DynamicItem
    {
        public Dictionary<string, AttributeValue> Raw { get; } = new();

        public Dictionary<string, string> Attrs { get; } = new();

        public static DynamicItem FromRaw(Dictionary<string, AttributeValue> raw)
        {
            var item = new DynamicItem();
            foreach (var kv in raw)
            {
                item.Raw[kv.Key] = kv.Value;
                item.Attrs[kv.Key] = kv.Value.S ?? kv.Value.N ?? kv.Value.BOOL.ToString() ?? "(complex)";
            }
            return item;
        }

        public string Get(string key) => Attrs.TryGetValue(key, out var v) ? v : "";
    }
}