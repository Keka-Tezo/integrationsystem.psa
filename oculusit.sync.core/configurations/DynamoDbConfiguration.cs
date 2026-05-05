namespace oculusit.sync.core.configurations;

public sealed class DynamoDbConfiguration
{
    public const string SectionName = "DynamoDB";

    public string TableName { get; init; } = "oculusit-sync-state";
}
