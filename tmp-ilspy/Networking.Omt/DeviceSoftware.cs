using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DeviceXmlUtil;
using Networking.Omt.Dto.DeviceSoftware;
using RestSharp;

namespace Networking.Omt;

internal sealed class DeviceSoftware : IDeviceSoftware
{
	private record GetUpdatesRequest(string GarminDeviceXml);

	private readonly OmtRestClient _restClient;

	private readonly ICache _cache;

	public DeviceSoftware(OmtRestClient restClient, ICache cache)
	{
		_restClient = restClient;
		_cache = cache;
	}

	public async Task<DeviceSoftwareUpdateResponse> GetMarineSoftwareUpdateAsync()
	{
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData("GetMarineSoftwareUpdateAsync");
		byte[] cacheKey = sha.GetHashAndReset();
		DeviceSoftwareUpdateResponse deviceSoftwareUpdateResponse = _cache.Get<DeviceSoftwareUpdateResponse>(cacheKey, TimeSpan.FromHours(1.0));
		if (deviceSoftwareUpdateResponse != null)
		{
			return deviceSoftwareUpdateResponse;
		}
		RestRequest request = new RestRequest("api/device-software/v1/products/marine/updates");
		RestResponse<DeviceSoftwareUpdateResponse> restResponse = await _restClient.ExecuteAsync<DeviceSoftwareUpdateResponse>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data;
	}

	public async Task<DeviceSoftwareUpdateResponse> GetMarineSoftwareUpdateAsync(XmlDevice xmlDevice)
	{
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData(xmlDevice.ToString());
		byte[] cacheKey = sha.GetHashAndReset();
		DeviceSoftwareUpdateResponse deviceSoftwareUpdateResponse = _cache.Get<DeviceSoftwareUpdateResponse>(cacheKey, TimeSpan.FromHours(1.0));
		if (deviceSoftwareUpdateResponse != null)
		{
			return deviceSoftwareUpdateResponse;
		}
		RestRequest request = new RestRequest("api/device-software/v1/products/marine/updates", Method.Post);
		request.AddJsonBody(new GetUpdatesRequest(xmlDevice.ToString()));
		RestResponse<DeviceSoftwareUpdateResponse> restResponse = await _restClient.ExecuteAsync<DeviceSoftwareUpdateResponse>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data;
	}
}
