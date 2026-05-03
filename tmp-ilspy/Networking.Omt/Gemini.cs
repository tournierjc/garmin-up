using System.Threading.Tasks;
using DeviceXmlUtil;
using Networking.Omt.Dto.Common;
using Networking.Omt.Dto.Gemini;
using RestSharp;

namespace Networking.Omt;

internal sealed class Gemini : IGemini
{
	private readonly OmtRestClient _restClient;

	private readonly XmlDevice _xmlDevice;

	public Gemini(OmtRestClient restClient, XmlDevice xmlDevice)
	{
		_restClient = restClient;
		_xmlDevice = xmlDevice;
	}

	public async Task<GetUpdatesResponse> GetUpdatesAsync()
	{
		var obj = new
		{
			UnitInfo = new FullUnitInfo(_xmlDevice)
		};
		RestRequest request = new RestRequest("/api/maps/gemini/updates", Method.Post);
		request.AddJsonBody(obj);
		return (await _restClient.ExecuteAsync<GetUpdatesResponse>(request)).Data;
	}

	public async Task<Networking.Omt.Dto.Gemini.ActivateUpdatesResponse> ActivateAsync(GeminiMapSectionIdentifier[] identifiers)
	{
		GeminiActivationRequest obj = new GeminiActivationRequest
		{
			UnitInfo = new FullUnitInfo(_xmlDevice),
			MapImages = identifiers
		};
		RestRequest request = new RestRequest("/api/maps/gemini/activate", Method.Post);
		request.AddJsonBody(obj);
		return (await _restClient.ExecuteAsync<Networking.Omt.Dto.Gemini.ActivateUpdatesResponse>(request)).Data;
	}
}
