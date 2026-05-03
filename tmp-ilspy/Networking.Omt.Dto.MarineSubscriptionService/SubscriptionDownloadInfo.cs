namespace Networking.Omt.Dto.MarineSubscriptionService;

public class SubscriptionDownloadInfo
{
	public required FileDownloadInfo[] FileDownloadInfos { get; init; }

	public required string ScrId { get; init; }
}
