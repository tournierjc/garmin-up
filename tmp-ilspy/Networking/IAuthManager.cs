using System.Threading.Tasks;
using Networking.Auth;
using Networking.Connect.Dto.UserProfileService;

namespace Networking;

public interface IAuthManager
{
	void AddAuth(UnitId unitId, DIToken diToken);

	void AddExistingAuth(ITAuth auth);

	void AddNewAuth(UnitId unitId, ConnectAuth auth, ConnectAccount? acct);

	void AddNewAuth(ITAuth auth);

	void DeleteAuth(UnitId unitId);

	void DeleteAuth(CustomerGuid customerGuid);

	Task<bool> ValidateITAuth(CustomerGuid customerGuid);

	void AddAuth(UnitId unitId, ITAuth auth);
}
