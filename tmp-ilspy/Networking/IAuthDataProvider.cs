using System.Threading.Tasks;
using Microsoft.IdentityModel.JsonWebTokens;
using Networking.Auth;
using Networking.Connect.Dto.UserProfileService;

namespace Networking;

public interface IAuthDataProvider
{
	bool HasConnectAuth(UnitId unitId);

	bool HasITAuth(CustomerGuid customerGuid);

	ConnectAuth? GetConnectAuth(UnitId unitId);

	Task<DIToken?> GetDIAuthAsync(UnitId unitId);

	Task<ITAuth?> GetITAuthAsync(UnitId unitId);

	Task<ITAuth?> GetITAuthAsync(CustomerGuid customerGuid);

	Task<JsonWebToken?> GetOmtAuthAsync(CustomerGuid customerGuid);

	void ExpireITAuth(CustomerGuid customerGuid);

	ConnectAccount? GetConnectAccount(UnitId unitId);

	CustomerGuid? GetCustomerGuid(UnitId unitId);
}
