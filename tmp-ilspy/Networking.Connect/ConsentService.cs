using System.Threading.Tasks;
using RestSharp;

namespace Networking.Connect;

internal class ConsentService : IConsentService
{
	private record SetConsentRequest(string ConsentTypeId, string ConsentLocale, string ConsentVersion);

	private readonly ConnectRestClient _restClient;

	private readonly CustomerGuid? _customerGuid;

	public ConsentService(ConnectRestClient restClient, CustomerGuid? customerGuid = null)
	{
		_restClient = restClient;
		_customerGuid = customerGuid;
	}

	public async Task<RestResponse> GetConsentAsync(string consentType)
	{
		RestRequest request = new RestRequest("/gdprconsent-service/feature/consent/" + consentType);
		return await _restClient.ExecuteAsync(request);
	}

	public async Task<RestResponse> SetConsentAsync(bool consented, string consentType, string locale, string version)
	{
		RestRequest request = new RestRequest("/gdprconsent-service/consent/" + (consented ? "grant" : "revoke"), Method.Post);
		request.AddJsonBody(new SetConsentRequest(consentType, locale, version));
		return await _restClient.ExecuteAsync(request);
	}

	public async Task<RestResponse> RevokeConsentAsync(string consentType, bool clientInitiated, string locale, string version, long userId, string passwordChallengeToken)
	{
		RestRequest request = new RestRequest("/gdprconsent-service/consent/revoke/" + consentType + "/" + clientInitiated.ToString().ToLower(), Method.Post);
		request.AddJsonBody(new SetConsentRequest(consentType, locale, version));
		request.AddHeader("USER_ID", userId);
		request.AddHeader("GARMIN_GUID", _customerGuid?.Guid.ToString() ?? string.Empty);
		request.AddHeader("passwordChallengeToken", passwordChallengeToken);
		return await _restClient.ExecuteAsync(request);
	}
}
