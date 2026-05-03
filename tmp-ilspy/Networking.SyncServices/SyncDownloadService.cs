using System;
using System.Threading.Tasks;
using Networking.Connect;
using RestSharp;

namespace Networking.SyncServices;

[NetworkingService(typeof(ISyncDownloadService))]
internal sealed class SyncDownloadService : ISyncDownloadService
{
	private readonly ConnectRestClient _restClient;

	public SyncDownloadService(ConnectRestClient restClient)
	{
		_restClient = restClient;
	}

	public async Task<RestResponse> DownloadFileAsync(Uri uri)
	{
		RestRequest request = new CustomLoggingRestRequest(uri, Method.Get, censorRequest: false);
		request.AddHeader("Accept", "application/octet-stream, application/json");
		try
		{
			return await _restClient.ExecuteAsync(request);
		}
		catch (RestRequestException ex)
		{
			return ex.Response;
		}
	}
}
