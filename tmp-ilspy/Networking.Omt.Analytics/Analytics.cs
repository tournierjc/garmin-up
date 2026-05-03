using System;
using System.Threading.Tasks;
using Networking.Omt.Dto;
using RestSharp;

namespace Networking.Omt.Analytics;

internal sealed class Analytics : IAnalytics
{
	private readonly OmtRestClient _restClient;

	private readonly Guid _installationGuid;

	public Analytics(OmtRestClient restClient, InstallationGuid installationGuid)
	{
		_restClient = restClient;
		_installationGuid = installationGuid.Guid;
	}

	public async Task SendCrashReportEventAsync(CrashReportEvent crashReportEvent)
	{
		RestRequest request = new RestRequest("Rce/ProtobufApi/analytics/errors", Method.Post);
		request.AddHeader("Garmin-Client-Guid", _installationGuid.ToString());
		request.AddJsonBody(crashReportEvent);
		await _restClient.ExecuteAsync(request);
	}
}
