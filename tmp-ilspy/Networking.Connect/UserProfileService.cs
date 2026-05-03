using System.Threading.Tasks;
using Networking.Auth;
using Networking.Connect.Dto.UserProfileService;
using RestSharp;

namespace Networking.Connect;

internal sealed class UserProfileService : IUserProfileService
{
	private readonly ConnectRestClient _restClient;

	public UserProfileService(ConnectRestClient restClient)
	{
		_restClient = restClient;
	}

	public async Task<ConnectAccount> GetConnectAccountAsync()
	{
		RestRequest request = new RestRequest("userprofile-service/userprofile/userProfileBase");
		return (await _restClient.ExecuteAsync<ConnectAccount>(request)).Data;
	}

	public async Task<UserLocationResponse> GetUserLocationAsync()
	{
		RestRequest request = new RestRequest("userprofile-service/userprofile/location");
		return (await _restClient.ExecuteAsync<UserLocationResponse>(request)).Data;
	}

	public async Task<ConnectSocialProfile> GetConnectSocialProfileAsync()
	{
		RestRequest request = new RestRequest("userprofile-service/socialProfile");
		return (await _restClient.ExecuteAsync<ConnectSocialProfile>(request)).Data;
	}

	public async Task<GetPrimaryTrainingDeviceIdResponse> GetPrimaryTrainingDeviceIdAsync(bool ignoreCache = false)
	{
		RestRequest request = new RestRequest("userprofile-service/userprofile/user-settings/primary-training-device");
		request.AddParameter("ignoreCache", ignoreCache);
		return (await _restClient.ExecuteAsync<GetPrimaryTrainingDeviceIdResponse>(request)).Data;
	}

	public async Task SetPrimaryTrainingDeviceAsync(uint deviceId)
	{
		RestRequest request = new RestRequest("userprofile-service/userprofile/user-settings/primary-training-device", Method.Put);
		request.AddJsonBody(new { deviceId });
		await _restClient.ExecuteAsync(request);
	}

	public async Task<string[]> SetUserRoleMarineAsync(ConnectAuth connectAuth)
	{
		RestRequest restRequest = new RestRequest("userprofile-service/userprofile/role/ROLE_MARINE_USER", Method.Post);
		restRequest.Authenticator = ConnectTokenProvider.GetAuthenticatorForAccessToken(connectAuth);
		return (await _restClient.ExecuteAsync<string[]>(restRequest)).Data;
	}

	public async Task<ConnectUserData> GetConnectUserSettingsAsync()
	{
		RestRequest request = new RestRequest("userprofile-service/userprofile/user-settings");
		return (await _restClient.ExecuteAsync<ConnectUserData>(request)).Data;
	}

	public async Task<RestResponse> SetConnectUserSettingsAsync(ConnectUserData data)
	{
		RestRequest request = new RestRequest("userprofile-service/userprofile/user-settings", Method.Put);
		request.AddJsonBody(data);
		return await _restClient.ExecuteAsync(request);
	}
}
