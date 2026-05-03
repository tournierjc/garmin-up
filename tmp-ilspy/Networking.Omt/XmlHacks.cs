using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DeviceXmlUtil;
using RestSharp;

namespace Networking.Omt;

internal sealed class XmlHacks : IXmlHacks
{
	private record RepairDeviceXml(string Content);

	private readonly OmtRestClient _restClient;

	private readonly ICache _cache;

	public XmlHacks(OmtRestClient restClient, ICache cache)
	{
		_restClient = restClient;
		_cache = cache;
	}

	public async Task<XmlDevice> GetRepairedDeviceXmlAsync(XmlDevice xmlDevice)
	{
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData("/Devices/xHaas/RepairDeviceXml");
		sha.AppendData(xmlDevice.ToString());
		byte[] cacheKey = sha.GetHashAndReset();
		RepairDeviceXml repairDeviceXml = _cache.Get<RepairDeviceXml>(cacheKey, TimeSpan.FromHours(1.0));
		if ((object)repairDeviceXml != null)
		{
			return Serializer.Deserialize(repairDeviceXml.Content);
		}
		RestRequest request = new RestRequest("/Devices/xHaas/RepairDeviceXml", Method.Post);
		request.AddJsonBody(new RepairDeviceXml(xmlDevice.ToString()));
		RestResponse<RepairDeviceXml> restResponse = await _restClient.ExecuteAsync<RepairDeviceXml>(request);
		_cache.Set(cacheKey, restResponse);
		return Serializer.Deserialize(restResponse.Data.Content);
	}
}
