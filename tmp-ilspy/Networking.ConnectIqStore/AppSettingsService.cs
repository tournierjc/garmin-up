using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace Networking.ConnectIqStore;

internal sealed class AppSettingsService : IAppSettingsService
{
	private readonly ConnectIqRestClient _restClient;

	public AppSettingsService(ConnectIqRestClient restClient)
	{
		_restClient = restClient;
	}

	public async Task<RestResponse> GetSavedAppSettings(string locale, Guid appId, uint version, string firmwarePartNumber, string javaScriptResponse)
	{
		RestRequest request = new RestRequest($"{locale}/appSettings2/{appId}/versions/{version}/devices/{firmwarePartNumber}/binary", Method.Post);
		request.AddHeader("Accept", "application/octet-stream");
		request.AddStringBody(javaScriptResponse, DataFormat.Json);
		return await _restClient.ExecuteAsync(request);
	}

	public async Task<RestResponse> GetPersistedAppSettingsHtml(string locale, Guid appId, uint version, string firmwarePartNumber, Dictionary<object, object> settingsDict)
	{
		RestRequest request = new RestRequest($"{locale}/appSettings2/{appId}/versions/{version}/devices/{firmwarePartNumber}/edit?singlePage=true", Method.Post);
		request.AddHeader("Accept", "application/json");
		request.AddStringBody(JsonConvert.SerializeObject(settingsDict), DataFormat.Json);
		return await _restClient.ExecuteAsync(request);
	}
}
