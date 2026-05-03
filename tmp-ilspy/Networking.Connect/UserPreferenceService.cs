using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.Connect.Dto.UserPreferenceService;
using RestSharp;

namespace Networking.Connect;

internal sealed class UserPreferenceService : IUserPreferenceService
{
	private readonly ConnectRestClient _restClient;

	public UserPreferenceService(ConnectRestClient restClient)
	{
		_restClient = restClient;
	}

	public async Task<bool> GetAccountDeviceUploadConsentAsync()
	{
		RestRequest request = new RestRequest("/userpreference-service/account.deviceSync");
		bool.TryParse((await _restClient.ExecuteAsync<KeyValuePair<string, string>>(request)).Data.Value, out var result);
		return result;
	}

	public async Task<AccountDeviceSyncResponse> SetAccountDeviceSync(bool consented)
	{
		RestRequest request = new RestRequest("/userpreference-service/", Method.Post);
		request.AddHeader("X-actor", "SYSTEM");
		request.AddJsonBody(new Dictionary<string, string>
		{
			["key"] = "account.deviceSync",
			["value"] = consented.ToString().ToLower()
		});
		return (await _restClient.ExecuteAsync<AccountDeviceSyncResponse>(request)).Data;
	}
}
