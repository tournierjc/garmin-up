using System;
using System.Threading.Tasks;
using Networking.Omt.Dto.MarineSubscriptionService;
using RestSharp;

namespace Networking.Omt;

internal sealed class MarineSubscriptionService : IMarineSubscriptionService
{
	private readonly OmtJwtRestClient _restClient;

	private readonly Guid _customerGuid;

	public MarineSubscriptionService(OmtJwtRestClient restClient, CustomerGuid customerGuid)
	{
		_restClient = restClient;
		_customerGuid = customerGuid.Guid;
	}

	public async Task<ChartSubscription> GetChartSubscriptions()
	{
		RestRequest request = new RestRequest($"/api/marine/customers/{_customerGuid}/chartsubscriptions");
		return (await _restClient.ExecuteAsync<ChartSubscription>(request)).Data;
	}

	public async Task<FileDownload> GetUpdatedChartFileDownload(ManifestFileInfo[] manifestFileInfos)
	{
		RestRequest request = new RestRequest($"/api/marine/customers/{_customerGuid}/chartsubscriptions/downloaddetails", Method.Post);
		request.AddJsonBody(manifestFileInfos);
		return (await _restClient.ExecuteAsync<FileDownload>(request)).Data;
	}

	public async Task<ChartSubscriptionUnlocks> GetChartSubscriptionUnlocks()
	{
		RestRequest request = new RestRequest($"/api/marine/customers/{_customerGuid}/chartsubscriptions/gmas");
		return (await _restClient.ExecuteAsync<ChartSubscriptionUnlocks>(request)).Data;
	}

	public async Task<ChartSubscriptionDownloadInfo> GetChartSubscriptionDownloadInfo(Chart chart)
	{
		RestRequest request = new RestRequest($"/api/marine/customers/{_customerGuid}/chartsubscriptions/{chart.ScrId}/downloadinfo");
		return (await _restClient.ExecuteAsync<ChartSubscriptionDownloadInfo>(request)).Data;
	}
}
