namespace Networking.Omt.Dto.MapUpdateService;

public class MapUpdateInfo
{
	public string? ProductKey { get; set; }

	public bool IsReinstall { get; set; }

	public string? PartNumber { get; set; }

	public MapUpdateType UpdateType { get; set; }

	public int MajorVersion { get; set; }

	public int MinorVersion { get; set; }

	public string? ProductGroup { get; set; }

	public string? DisplayName { get; set; }

	public string? EulaUrl { get; set; }

	public bool CanAutoStartDownload { get; set; }

	public string? ReleaseNotes { get; set; }
}
