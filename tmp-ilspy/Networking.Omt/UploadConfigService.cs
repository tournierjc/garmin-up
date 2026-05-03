using System;
using System.Threading.Tasks;
using Networking.Omt.Dto.UploadConfigService;
using RestSharp;

namespace Networking.Omt;

[NetworkingService(typeof(IUploadConfigService))]
internal class UploadConfigService : IUploadConfigService
{
	private readonly OmtRestClient _restClient;

	private readonly UnitId _unitId;

	public UploadConfigService(OmtRestClient restClient, UnitId unitId)
	{
		_restClient = restClient;
		_unitId = unitId;
	}

	public async Task<UnitUploadSettings> GetUnitUploadSettingsAsync(string clientId, Guid clientGuid, bool enforceConsent)
	{
		RestRequest request = new RestRequest($"UploadConfigurationService/UnitUploadSettings/{_unitId.Id}?clientId={clientId}&enforceConsent={enforceConsent}");
		request.AddHeader("Garmin-Client-Guid", clientGuid);
		return (await _restClient.ExecuteAsync<UnitUploadSettings>(request)).Data;
	}
}
