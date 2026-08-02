using System.Text.Json.Serialization;

namespace OneDriveVersionCleaner.Infrastructure.Graph;

internal sealed record GraphCollection<T>
{
    [JsonPropertyName("value")]
    public IReadOnlyList<T> Value { get; init; } = [];

    [JsonPropertyName("@odata.nextLink")]
    public string? NextLink { get; init; }
}

internal sealed record GraphUser
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("displayName")]
    public string DisplayName { get; init; } = "";

    [JsonPropertyName("userPrincipalName")]
    public string UserPrincipalName { get; init; } = "";
}

internal sealed record GraphDrive
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("driveType")]
    public string DriveType { get; init; } = "";

    [JsonPropertyName("webUrl")]
    public string WebUrl { get; init; } = "";
}

internal sealed record GraphDriveItem
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("webUrl")]
    public string WebUrl { get; init; } = "";

    [JsonPropertyName("size")]
    public long? Size { get; init; }

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get; init; }

    [JsonPropertyName("lastModifiedBy")]
    public GraphIdentitySet? LastModifiedBy { get; init; }

    [JsonPropertyName("folder")]
    public GraphFolderFacet? Folder { get; init; }

    [JsonPropertyName("file")]
    public GraphFileFacet? File { get; init; }

    [JsonPropertyName("parentReference")]
    public GraphParentReference? ParentReference { get; init; }

    [JsonPropertyName("remoteItem")]
    public GraphRemoteItemFacet? RemoteItem { get; init; }
}

internal sealed record GraphRemoteItemFacet;

internal sealed record GraphFolderFacet
{
    [JsonPropertyName("childCount")]
    public int? ChildCount { get; init; }
}

internal sealed record GraphFileFacet
{
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }
}

internal sealed record GraphParentReference
{
    [JsonPropertyName("driveId")]
    public string? DriveId { get; init; }

    [JsonPropertyName("path")]
    public string? Path { get; init; }
}

internal sealed record GraphIdentitySet
{
    [JsonPropertyName("user")]
    public GraphIdentity? User { get; init; }

    [JsonPropertyName("application")]
    public GraphIdentity? Application { get; init; }
}

internal sealed record GraphIdentity
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }
}

internal sealed record GraphDriveItemVersion
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("lastModifiedDateTime")]
    public DateTimeOffset? LastModifiedDateTime { get; init; }

    [JsonPropertyName("lastModifiedBy")]
    public GraphIdentitySet? LastModifiedBy { get; init; }

    [JsonPropertyName("size")]
    public long? Size { get; init; }

}

internal sealed record GraphErrorEnvelope
{
    [JsonPropertyName("error")]
    public GraphError? Error { get; init; }
}

internal sealed record GraphError
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}
