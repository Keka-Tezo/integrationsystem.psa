using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using oculusit.sync.core.configurations;
using oculusit.sync.core.interfaces;
using oculusit.sync.core.models;

namespace oculusit.sync.core.services;

public sealed class DynamoDbSyncStateService(
    IAmazonDynamoDB dynamoDb,
    IOptions<DynamoDbConfiguration> options,
    ILogger<DynamoDbSyncStateService> logger) : ISyncStateService
{
    private const string KeyAttribute              = "syncType";
    private const string LastUpdatedAtAttribute    = "lastUpdatedAt";
    private const string CompaniesAttribute        = "companies";
    private const string ProjectsAttribute         = "projects";
    private const string FailedProjectsAttribute   = "failedProjects";
    private const string IdAttribute               = "id";
    private const string ClientIdAttribute         = "clientId";
    private const string KekaClientIdAttribute     = "kekaClientId";
    private const string KekaProjectIdAttribute    = "kekaProjectId";
    private const string NameAttribute             = "name";
    private const string ErrorMessageAttribute     = "errorMessage";

    private readonly string _tableName = options.Value.TableName;

    public async Task<SyncState?> GetAsync(string syncType, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Reading DynamoDB sync state for syncType={SyncType} from table {Table}.", syncType, _tableName);

        var request = new GetItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [KeyAttribute] = new AttributeValue { S = syncType }
            }
        };

        var response = await dynamoDb.GetItemAsync(request, cancellationToken);

        if (!response.IsItemSet)
        {
            logger.LogDebug("No sync state found for syncType={SyncType}.", syncType);
            return null;
        }

        DateTime? lastUpdatedAt = null;
        if (response.Item.TryGetValue(LastUpdatedAtAttribute, out var tsAttr)
            && DateTime.TryParse(tsAttr.S, out var parsed))
        {
            lastUpdatedAt = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
        }

        var companies = new List<SyncedCompanyEntry>();
        if (response.Item.TryGetValue(CompaniesAttribute, out var listAttr) && listAttr.L is { Count: > 0 })
        {
            foreach (var entry in listAttr.L)
            {
                if (entry.M is null) continue;
                entry.M.TryGetValue(IdAttribute, out var idAttr);
                entry.M.TryGetValue(ClientIdAttribute, out var clientIdAttr);
                companies.Add(new SyncedCompanyEntry
                {
                    Id       = idAttr?.S ?? string.Empty,
                    ClientId = clientIdAttr?.S ?? string.Empty
                });
            }
        }

        var projects = new List<SyncedProjectEntry>();
        if (response.Item.TryGetValue(ProjectsAttribute, out var projectsAttr) && projectsAttr.L is { Count: > 0 })
        {
            foreach (var entry in projectsAttr.L)
            {
                if (entry.M is null) continue;
                entry.M.TryGetValue(IdAttribute, out var idAttr);
                entry.M.TryGetValue(KekaClientIdAttribute, out var kekaClientIdAttr);
                entry.M.TryGetValue(KekaProjectIdAttribute, out var kekaProjectIdAttr);
                projects.Add(new SyncedProjectEntry
                {
                    Id            = idAttr?.S ?? string.Empty,
                    KekaClientId  = kekaClientIdAttr?.S,
                    KekaProjectId = kekaProjectIdAttr?.S
                });
            }
        }

        return new SyncState
        {
            SyncType       = syncType,
            LastUpdatedAt  = lastUpdatedAt,
            Companies      = companies,
            Projects       = projects,
            FailedProjects = await ReadFailedProjectsAsync(response.Item)
        };
    }

    public async Task SaveAsync(SyncState state, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Saving DynamoDB sync state for syncType={SyncType} to table {Table}.", state.SyncType, _tableName);

        var item = new Dictionary<string, AttributeValue>
        {
            [KeyAttribute] = new AttributeValue { S = state.SyncType }
        };

        if (state.LastUpdatedAt.HasValue)
            item[LastUpdatedAtAttribute] = new AttributeValue { S = state.LastUpdatedAt.Value.ToString("o") };

        if (state.Companies.Count > 0)
        {
            item[CompaniesAttribute] = new AttributeValue
            {
                L = state.Companies.Select(c => new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
                    {
                        [IdAttribute]       = new AttributeValue { S = c.Id },
                        [ClientIdAttribute] = new AttributeValue { S = c.ClientId }
                    }
                }).ToList()
            };
        }

        if (state.Projects.Count > 0)
        {
            item[ProjectsAttribute] = new AttributeValue
            {
                L = state.Projects.Select(p => new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
                    {
                        [IdAttribute]           = new AttributeValue { S = p.Id },
                        [KekaClientIdAttribute] = new AttributeValue { S = p.KekaClientId ?? string.Empty },
                        [KekaProjectIdAttribute]= new AttributeValue { S = p.KekaProjectId ?? string.Empty }
                    }
                }).ToList()
            };
        }

        if (state.FailedProjects.Count > 0)
        {
            item[FailedProjectsAttribute] = new AttributeValue
            {
                L = state.FailedProjects.Select(p => new AttributeValue
                {
                    M = new Dictionary<string, AttributeValue>
                    {
                        [IdAttribute]           = new AttributeValue { S = p.Id },
                        [NameAttribute]         = new AttributeValue { S = p.Name },
                        [ErrorMessageAttribute] = new AttributeValue { S = p.ErrorMessage }
                    }
                }).ToList()
            };
        }

        var request = new PutItemRequest
        {
            TableName = _tableName,
            Item      = item
        };

        await dynamoDb.PutItemAsync(request, cancellationToken);

        logger.LogInformation("Saved sync state for syncType={SyncType}, lastUpdatedAt={LastUpdatedAt}, companies={Count}.",
            state.SyncType, state.LastUpdatedAt, state.Companies.Count);
    }

    public async Task AppendCompaniesAsync(
        string syncType,
        IReadOnlyList<SyncedCompanyEntry> newEntries,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Appending {Count} company entries to DynamoDB for syncType={SyncType}.", newEntries.Count, syncType);

        var newItems = newEntries.Select(c => new AttributeValue
        {
            M = new Dictionary<string, AttributeValue>
            {
                [IdAttribute]       = new AttributeValue { S = c.Id },
                [ClientIdAttribute] = new AttributeValue { S = c.ClientId }
            }
        }).ToList();

        var updateRequest = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [KeyAttribute] = new AttributeValue { S = syncType }
            },
            // list_append appends newItems to the existing companies list.
            // if_not_exists handles the edge case where companies attribute doesn't exist yet.
            UpdateExpression = "SET #companies = list_append(if_not_exists(#companies, :empty), :newItems), #lastUpdatedAt = :lastUpdatedAt",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#companies"]     = CompaniesAttribute,
                ["#lastUpdatedAt"] = LastUpdatedAtAttribute
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":newItems"]      = new AttributeValue { L = newItems },
                [":empty"]         = new AttributeValue { L = [] },
                [":lastUpdatedAt"] = new AttributeValue { S = lastUpdatedAt.ToString("o") }
            }
        };

        await dynamoDb.UpdateItemAsync(updateRequest, cancellationToken);

        logger.LogInformation("Appended {Count} company entries and updated lastUpdatedAt={LastUpdatedAt} for syncType={SyncType}.",
            newEntries.Count, lastUpdatedAt, syncType);
    }

    public async Task AppendProjectsAsync(
        string syncType,
        IReadOnlyList<SyncedProjectEntry> newEntries,
        DateTime lastUpdatedAt,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Appending {Count} project entries to DynamoDB for syncType={SyncType}.", newEntries.Count, syncType);

        var newItems = newEntries.Select(p => new AttributeValue
        {
            M = new Dictionary<string, AttributeValue>
            {
                [IdAttribute]           = new AttributeValue { S = p.Id },
                [KekaClientIdAttribute] = new AttributeValue { S = p.KekaClientId ?? string.Empty },
                [KekaProjectIdAttribute]= new AttributeValue { S = p.KekaProjectId ?? string.Empty }
            }
        }).ToList();

        var updateRequest = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [KeyAttribute] = new AttributeValue { S = syncType }
            },
            UpdateExpression = "SET #projects = list_append(if_not_exists(#projects, :empty), :newItems), #lastUpdatedAt = :lastUpdatedAt",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#projects"]      = ProjectsAttribute,
                ["#lastUpdatedAt"] = LastUpdatedAtAttribute
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":newItems"]      = new AttributeValue { L = newItems },
                [":empty"]         = new AttributeValue { L = [] },
                [":lastUpdatedAt"] = new AttributeValue { S = lastUpdatedAt.ToString("o") }
            }
        };

        await dynamoDb.UpdateItemAsync(updateRequest, cancellationToken);

        logger.LogInformation("Appended {Count} project entries and updated lastUpdatedAt={LastUpdatedAt} for syncType={SyncType}.",
            newEntries.Count, lastUpdatedAt, syncType);
    }

    public async Task SaveFailedProjectsAsync(
        string syncType,
        IReadOnlyList<FailedProjectEntry> failedEntries,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Saving {Count} failed project entries to DynamoDB for syncType={SyncType}.", failedEntries.Count, syncType);

        var failedItems = failedEntries.Select(p => new AttributeValue
        {
            M = new Dictionary<string, AttributeValue>
            {
                [IdAttribute]           = new AttributeValue { S = p.Id },
                [NameAttribute]         = new AttributeValue { S = p.Name },
                [ErrorMessageAttribute] = new AttributeValue { S = p.ErrorMessage }
            }
        }).ToList();

        var updateRequest = new UpdateItemRequest
        {
            TableName = _tableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [KeyAttribute] = new AttributeValue { S = syncType }
            },
            // Overwrite the failedProjects list each run so it always reflects the latest failures.
            UpdateExpression = "SET #failedProjects = :failedItems",
            ExpressionAttributeNames = new Dictionary<string, string>
            {
                ["#failedProjects"] = FailedProjectsAttribute
            },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":failedItems"] = new AttributeValue { L = failedItems }
            }
        };

        await dynamoDb.UpdateItemAsync(updateRequest, cancellationToken);

        logger.LogInformation("Saved {Count} failed project entries for syncType={SyncType}.", failedEntries.Count, syncType);
    }

    private static Task<IReadOnlyList<FailedProjectEntry>> ReadFailedProjectsAsync(
        Dictionary<string, AttributeValue> item)
    {
        var failed = new List<FailedProjectEntry>();

        if (item.TryGetValue(FailedProjectsAttribute, out var attr) && attr.L is { Count: > 0 })
        {
            foreach (var entry in attr.L)
            {
                if (entry.M is null) continue;
                entry.M.TryGetValue(IdAttribute, out var idAttr);
                entry.M.TryGetValue(NameAttribute, out var nameAttr);
                entry.M.TryGetValue(ErrorMessageAttribute, out var errAttr);
                failed.Add(new FailedProjectEntry
                {
                    Id           = idAttr?.S ?? string.Empty,
                    Name         = nameAttr?.S ?? string.Empty,
                    ErrorMessage = errAttr?.S ?? string.Empty
                });
            }
        }

        return Task.FromResult<IReadOnlyList<FailedProjectEntry>>(failed);
    }
}
