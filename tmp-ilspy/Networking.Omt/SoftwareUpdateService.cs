using System;
using System.Threading.Tasks;
using DeviceXmlUtil;
using Networking.Omt.Dto.SoftwareUpdateService;
using RestSharp;

namespace Networking.Omt;

internal sealed class SoftwareUpdateService : ISoftwareUpdateService
{
	private readonly OmtRestClient _restClient;

	private readonly Func<XmlDevice> _xmlDeviceFunc;

	public SoftwareUpdateService(OmtRestClient restClient, Func<XmlDevice> xmlDeviceFunc)
	{
		_restClient = restClient;
		_xmlDeviceFunc = xmlDeviceFunc;
	}

	public async Task<AllUnitSoftwareUpdatesResponse> GetAllUnitSoftwareUpdatesAsync(bool isUserInteractive)
	{
		AllUnitSoftwareUpdatesRequest obj = new AllUnitSoftwareUpdatesRequest
		{
			IsUserInteractive = isUserInteractive,
			GarminDeviceXml = _xmlDeviceFunc().ToString()
		};
		RestRequest request = new RestRequest("/api/device-software/v1/updates", Method.Post);
		request.AddJsonBody(obj);
		return (await _restClient.ExecuteAsync<AllUnitSoftwareUpdatesResponse>(request)).Data;
	}
}
