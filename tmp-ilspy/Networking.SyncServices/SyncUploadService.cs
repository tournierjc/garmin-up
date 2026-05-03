using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Networking.Auth;
using Networking.Connect;
using Networking.SyncServices.Dto;
using RestSharp;

namespace Networking.SyncServices;

internal sealed class SyncUploadService : ISyncUploadService
{
	private readonly ILoggerFactory _loggerFactory;

	private readonly NetworkingRestClient _restClient;

	private readonly IAuthDataProvider _authDataProvider;

	private readonly UnitId _unitId;

	public SyncUploadService(ILoggerFactory loggerFactory, IAuthDataProvider authDataProvider, UnitId unitId)
	{
		_loggerFactory = loggerFactory;
		_restClient = new NetworkingRestClient(loggerFactory.CreateLogger<NetworkingRestClient>());
		_authDataProvider = authDataProvider;
		_unitId = unitId;
	}

	public IRestClient GetSyncRestClient()
	{
		return new NetworkingRestClient(_loggerFactory.CreateLogger<NetworkingRestClient>(), delegate(RestClientOptions i)
		{
			ConnectAuth auth = _authDataProvider.GetConnectAuth(_unitId) ?? throw new ConnectAuthorizationException();
			i.Authenticator = ConnectTokenProvider.GetAuthenticatorForAccessToken(auth);
		});
	}

	public async Task<UploadResponse?> UploadItemAsync(AuthType authType, UploadType uploadType, Uri uploadUri, byte[] fileToUpload, bool isLastFile)
	{
		RestRequest restRequest = new RestRequest(uploadUri, Method.Post);
		if (isLastFile)
		{
			restRequest.AddHeader("lastFile", "true");
		}
		if (uploadType == UploadType.Zip)
		{
			restRequest.AddHeader("Content-Type", "multipart/form-data");
			restRequest.AddFile("data", fileToUpload, string.Empty, "application/zip");
		}
		else
		{
			restRequest.AddHeader("Content-Type", "application/octet-stream");
			restRequest.AddBody(fileToUpload, ContentType.Binary);
		}
		switch (authType)
		{
		case AuthType.Connect:
		{
			ConnectAuth auth = _authDataProvider.GetConnectAuth(_unitId) ?? throw new ConnectAuthorizationException();
			restRequest.Authenticator = ConnectTokenProvider.GetAuthenticatorForAccessToken(auth);
			break;
		}
		case AuthType.Health:
		{
			DIToken dIToken = (await _authDataProvider.GetDIAuthAsync(_unitId)) ?? throw new DIAuthorizationException();
			restRequest.AddHeader("Authorization", "Bearer " + dIToken.AccessToken);
			break;
		}
		default:
			throw new ArgumentOutOfRangeException("authType", authType, null);
		case AuthType.None:
			break;
		}
		RestResponse<UploadResponse> restResponse = await _restClient.ExecuteAsync<UploadResponse>(restRequest);
		if ((object)restResponse.Data == null || string.IsNullOrEmpty(restResponse.Content))
		{
			return new UploadResponse(restResponse.StatusCode);
		}
		int? activityUploadDelay = null;
		string text = restResponse.Headers.FirstOrDefault((HeaderParameter x) => x.Name == "location-in-milliseconds")?.Value;
		if (text != null && int.TryParse(text, out var result))
		{
			activityUploadDelay = result;
		}
		Uri activityUploadLocation = null;
		string text2 = restResponse.Headers.FirstOrDefault((HeaderParameter x) => x.Name == "Location")?.Value;
		if (text2 != null)
		{
			activityUploadLocation = new Uri(text2);
		}
		return restResponse.Data with
		{
			ActivityUploadDelay = activityUploadDelay,
			ActivityUploadLocation = activityUploadLocation,
			ResponseCode = (restResponse.Data.ResponseCode ?? restResponse.StatusCode)
		};
	}
}
