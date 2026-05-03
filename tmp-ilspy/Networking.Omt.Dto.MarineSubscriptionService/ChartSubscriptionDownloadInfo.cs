namespace Networking.Omt.Dto.MarineSubscriptionService;

public class ChartSubscriptionDownloadInfo
{
	public required CompressedFileDownloadInfo[] CompressedFileDownloadInfos { get; init; }

	public required ManifestFileDownloadInfo ManifestFileDownloadInfo { get; init; }
}
