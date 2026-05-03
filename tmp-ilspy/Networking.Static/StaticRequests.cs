using System.Collections.Generic;
using System.Threading.Tasks;
using RestSharp;

namespace Networking.Static;

internal sealed class StaticRequests : IStaticRequests
{
	private readonly StaticRestClient _restClient;

	public StaticRequests(StaticRestClient restClient)
	{
		_restClient = restClient;
	}

	public async Task<Dictionary<string, int>> GetSsoMinimumAges()
	{
		RestRequest request = new RestRequest("com.garmin.sso/createAccount/countryAgeMapping.json");
		return (await _restClient.ExecuteAsync<Dictionary<string, int>>(request)).Data;
	}
}
