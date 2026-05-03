using System;

namespace Networking.ITServices.Dto.ConnectIq;

public record IqItemResponse
{
	public Guid AppId { get; init; }

	public string? DeveloperName { get; init; }

	public string? Name { get; init; }

	public AppType Type { get; init; }

	public long Size { get; init; }

	public uint LatestInternalVersionNumber { get; init; }

	public string? LatestVersionName { get; init; }

	public bool PermissionsChanged { get; init; }

	public Permission[]? Permissions { get; init; }

	public bool? HasSettings { get; init; }
}
