using System.Threading.Tasks;
using Networking.Auth;
using Networking.Connect.Dto.UserProfileService;
using RestSharp;

namespace Networking.Connect;

public interface IUserProfileService
{
	Task<ConnectAccount> GetConnectAccountAsync();

	Task<UserLocationResponse> GetUserLocationAsync();

	Task<ConnectSocialProfile> GetConnectSocialProfileAsync();

	Task SetPrimaryTrainingDeviceAsync(uint deviceId);

	Task<GetPrimaryTrainingDeviceIdResponse> GetPrimaryTrainingDeviceIdAsync(bool ignoreCache = false);

	Task<string[]> SetUserRoleMarineAsync(ConnectAuth connectAuth);

	Task<ConnectUserData> GetConnectUserSettingsAsync();

	Task<RestResponse> SetConnectUserSettingsAsync(ConnectUserData data);
}
