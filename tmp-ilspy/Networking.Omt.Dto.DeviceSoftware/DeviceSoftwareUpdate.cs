using System;

namespace Networking.Omt.Dto.DeviceSoftware;

public class DeviceSoftwareUpdate
{
	public string? PartNumber { get; init; }

	public string? ProductName { get; init; }

	public string? Version { get; init; }

	public bool IsPrimaryFirmware { get; init; }

	public string? DataType { get; init; }

	public string? Description { get; init; }

	public string? Locale { get; init; }

	public string? Severity { get; init; }

	public DeviceSoftwareUpdateReleaseNote[]? ReleaseNotes { get; init; }

	public string? EulaUrl { get; init; }

	public DeviceSoftwareUpdateDeliverable? Deliverable { get; init; }

	public DeviceSoftwareUpdateInstallation? Installation { get; init; }

	public object[]? DeliveryRestrictions { get; init; }

	public string? ReleaseState { get; init; }

	public DateTime ReleaseDate { get; init; }

	public required Version SemanticVersion { get; init; }

	public string? DisplayVersion { get; init; }
}
