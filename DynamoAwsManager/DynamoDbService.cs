using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Configuration;

namespace DynamoAwsManager
{
    public class DynamoDbService
    {
        private readonly AmazonDynamoDBClient _client;

        public DynamoDbService()
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            _client = new AmazonDynamoDBClient(
                config["AWS:AccessKey"],
                config["AWS:SecretKey"],
                RegionEndpoint.GetBySystemName(config["AWS:Region"])
            );
        }

        public async Task<List<string>> ListTablesAsync()
        {
            var tables = new List<string>();
            string? lastTableName = null;

            do
            {
                var request = new ListTablesRequest { ExclusiveStartTableName = lastTableName };
                var response = await _client.ListTablesAsync(request);
                tables.AddRange(response.TableNames);
                lastTableName = response.LastEvaluatedTableName;
            }
            while (lastTableName != null);

            return tables;
        }

        public async Task<TableDescription> DescribeTableAsync(string tableName)
        {
            var response = await _client.DescribeTableAsync(tableName);
            return response.Table;
        }

        public async Task CreateTableAsync(string tableName, string hashKeyName, string hashKeyType,
                                           string? rangeKeyName = null, string? rangeKeyType = null)
        {
            var keySchema = new List<KeySchemaElement>
            {
                new KeySchemaElement(hashKeyName, KeyType.HASH)
            };

            var attrDefs = new List<AttributeDefinition>
            {
                new AttributeDefinition(hashKeyName, hashKeyType == "String" ? ScalarAttributeType.S : ScalarAttributeType.N)
            };

            if (!string.IsNullOrWhiteSpace(rangeKeyName) && !string.IsNullOrWhiteSpace(rangeKeyType))
            {
                keySchema.Add(new KeySchemaElement(rangeKeyName, KeyType.RANGE));
                attrDefs.Add(new AttributeDefinition(rangeKeyName,
                    rangeKeyType == "String" ? ScalarAttributeType.S : ScalarAttributeType.N));
            }

            var request = new CreateTableRequest
            {
                TableName = tableName,
                KeySchema = keySchema,
                AttributeDefinitions = attrDefs,
                BillingMode = BillingMode.PAY_PER_REQUEST
            };

            await _client.CreateTableAsync(request);
        }

        public async Task DeleteTableAsync(string tableName)
            => await _client.DeleteTableAsync(tableName);

        public async Task WaitUntilTableActiveAsync(string tableName)
        {
            while (true)
            {
                var desc = await DescribeTableAsync(tableName);
                if (desc.TableStatus == TableStatus.ACTIVE) break;
                await Task.Delay(1000);
            }
        }

        public async Task<List<Dictionary<string, AttributeValue>>> ScanTableAsync(string tableName)
        {
            var items = new List<Dictionary<string, AttributeValue>>();
            Dictionary<string, AttributeValue>? lastKey = null;

            do
            {
                var request = new ScanRequest
                {
                    TableName = tableName,
                    ExclusiveStartKey = lastKey
                };
                var response = await _client.ScanAsync(request);
                items.AddRange(response.Items);
                lastKey = response.LastEvaluatedKey?.Count > 0 ? response.LastEvaluatedKey : null;
            }
            while (lastKey != null);

            return items;
        }

        public async Task PutItemAsync(string tableName, Dictionary<string, AttributeValue> item)
        {
            await _client.PutItemAsync(new PutItemRequest
            {
                TableName = tableName,
                Item = item
            });
        }

        public async Task DeleteItemAsync(string tableName, Dictionary<string, AttributeValue> key)
        {
            await _client.DeleteItemAsync(new DeleteItemRequest
            {
                TableName = tableName,
                Key = key
            });
        }
    }
}