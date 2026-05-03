using System.Threading.Tasks;
using Networking.Connect.Dto.UserPreferenceService;

namespace Networking.Connect;

public interface IUserPreferenceService
{
	Task<bool> GetAccountDeviceUploadConsentAsync();

	Task<AccountDeviceSyncResponse> SetAccountDeviceSync(bool consented);
}
