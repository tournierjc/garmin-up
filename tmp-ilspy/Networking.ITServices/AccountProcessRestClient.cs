using Microsoft.Extensions.Logging;
using Networking.Auth;

namespace Networking.ITServices;

internal class AccountProcessRestClient : ITRestClient
{
	public AccountProcessRestClient(ILogger<ITRestClient> logger, GarminEnvironment garminEnvironment, ClientId clientId, IAuthDataProvider auth, ITAuth? itAuth = null, UnitId? unitId = null, CustomerGuid? customerGuid = null)
		: base(logger, garminEnvironment, clientId, auth, GetBaseUrl(garminEnvironment), itAuth, unitId, customerGuid)
	{
	}

	public new static string GetBaseUrl(GarminEnvironment garminEnvironment)
	{
		if (garminEnvironment == GarminEnvironment.ChinaTest)
		{
			return "https://account-test-china.customer.services.garmin.com";
		}
		return ITRestClient.GetBaseUrl(garminEnvironment);
	}
}
