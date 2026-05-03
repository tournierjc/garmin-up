using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.UniversalMaps;

public class BundledMap
{
	public required Release Release { get; init; }

	public required string DisplayName { get; init; }

	public required string MapType { get; init; }

	public required bool isReinstall { get; init; }

	public required string PurchasableUpdatePartNumber { get; init; }

	public required string[] EulaUrls { get; init; }

	public required DownloadHosts DownloadHosts { get; init; }

	public required ComputerInstall ComputerInstall { get; init; }

	public required GeographicRegion GeographicRegion { get; init; }
}
