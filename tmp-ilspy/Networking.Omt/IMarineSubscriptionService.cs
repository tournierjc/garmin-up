using System.Threading.Tasks;
using Networking.Omt.Dto.MarineSubscriptionService;

namespace Networking.Omt;

public interface IMarineSubscriptionService
{
	Task<ChartSubscription> GetChartSubscriptions();

	Task<FileDownload> GetUpdatedChartFileDownload(ManifestFileInfo[] manifestFileInfos);

	Task<ChartSubscriptionUnlocks> GetChartSubscriptionUnlocks();

	Task<ChartSubscriptionDownloadInfo> GetChartSubscriptionDownloadInfo(Chart chart);
}
