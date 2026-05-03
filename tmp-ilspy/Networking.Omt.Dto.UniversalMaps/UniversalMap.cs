using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.UniversalMaps;

public record UniversalMap
{
	public Release? Release { get; init; }

	public string? DisplayName { get; init; }

	public string? MapType { get; init; }

	public FileToRemove[]? FilesToRemove { get; init; }

	public MapInstallOption[]? InstallOptions { get; init; }

	public bool IsReinstall { get; init; }

	public bool CanUninstall { get; init; }

	public string? PurchasableUpdatePartNumber { get; init; }

	public string[]? EulaUrls { get; init; }

	public DownloadHosts? DownloadHosts { get; init; }

	public InstallationState InstallationState { get; init; }

	public ComputerInstall? ComputerInstall { get; init; }

	public Restriction? Restriction { get; init; }

	public FileCleanupRule[]? FileCleanupRules { get; init; }

	public GeographicRegion? GeographicRegion { get; init; }

	public bool RequiredForGeographicAreaGrouping { get; init; }
}
