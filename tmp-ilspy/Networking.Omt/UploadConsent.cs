using System.Threading.Tasks;
using RestSharp;

namespace Networking.Omt;

internal class UploadConsent : IUploadConsent
{
	private readonly OmtRestClient _restClient;

	private readonly UnitId _unitId;

	public UploadConsent(OmtRestClient restClient, UnitId unitId)
	{
		_restClient = restClient;
		_unitId = unitId;
	}

	public async Task<RestResponse> GetUnitConsents(string clientId)
	{
		RestRequest request = new RestRequest($"/api/upload-consent/units/{_unitId.Id}/consents");
		request.AddHeader("Garmin-Client-Guid", clientId);
		return await _restClient.ExecuteAsync(request);
	}

	public async Task<RestResponse> SetUnitConsent(string consentType, string consentState, string locale, string version, string clientId)
	{
		RestRequest request = new RestRequest($"/api/upload-consent/units/{_unitId.Id}/consents/{consentType}/versions/{version}/state", Method.Put);
		request.AddHeader("Garmin-Client-Guid", clientId);
		request.AddJsonBody(new
		{
			state = consentState,
			localeCode = locale
		});
		return await _restClient.ExecuteAsync(request);
	}
}
