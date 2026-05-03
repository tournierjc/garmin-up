using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DeviceXmlUtil;
using Networking.Omt.Dto.Common;
using Networking.Omt.Dto.UnitService;
using RestSharp;

namespace Networking.Omt;

internal sealed class UnitService : IUnitService
{
	private record GetUnitDisplayInfoRequest(BasicUnitInfo BasicUnitInfo)
	{
		public object ClientInfo => new object();
	}

	private record AssociateVehicleRequest(BasicUnitInfo BasicUnitInfo, string Vin)
	{
		public object ClientInfo => new object();
	}

	private record BasicUnitServiceRequest(BasicUnitInfo BasicUnitInfo)
	{
		public object ClientInfo => new
		{
			LocaleCode = CultureInfo.CurrentCulture.Name
		};
	}

	public record UnitManualsResponse(UnitManual[] Manuals);

	private readonly OmtRestClient _restClient;

	private readonly ICache _cache;

	private readonly UnitId _unitId;

	public UnitService(OmtRestClient restClient, ICache cache, UnitId unitId)
	{
		_restClient = restClient;
		_cache = cache;
		_unitId = unitId;
	}

	public async Task<UnitDisplayInfo> GetUnitDisplayInfoAsync(string? firmwarePartNumber)
	{
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData("Taiwan");
		sha.AppendData(_unitId.Id);
		if (firmwarePartNumber != null)
		{
			sha.AppendData(firmwarePartNumber);
		}
		byte[] cacheKey = sha.GetHashAndReset();
		UnitDisplayInfo unitDisplayInfo = _cache.Get<UnitDisplayInfo>(cacheKey, TimeSpan.FromDays(7.0));
		if (unitDisplayInfo != null)
		{
			return unitDisplayInfo;
		}
		RestRequest request = new RestRequest(string.Format("/api/unit-info/manufacturers/{0}/unitids/{1}", "Taiwan", _unitId));
		if (firmwarePartNumber != null)
		{
			request.AddParameter("primaryFirmwarePartNumber", firmwarePartNumber);
		}
		UnitDisplayInfo unitDisplayInfo2 = default(UnitDisplayInfo);
		try
		{
			RestResponse<UnitDisplayInfo> restResponse = await _restClient.ExecuteAsync<UnitDisplayInfo>(request);
			_cache.Set(cacheKey, restResponse);
			return restResponse.Data;
		}
		catch (RestRequestException) when (((Func<bool>)delegate
		{
			// Could not convert BlockContainer to single expression
			unitDisplayInfo2 = _cache.Get<UnitDisplayInfo>(cacheKey, TimeSpan.MaxValue);
			return unitDisplayInfo2 != null;
		}).Invoke())
		{
			return unitDisplayInfo2;
		}
	}

	public async Task RegisterContactForUnitAsync(RegisterContactForUnitRequest request)
	{
		RestRequest request2 = new RestRequest("/Rce/ProtobufApi/UnitService/RegisterContactForUnit", Method.Post);
		request2.AddJsonBody(request);
		await _restClient.ExecuteAsync(request2);
	}

	public async Task AssociateVehicleAsync(XmlDevice xmlDevice)
	{
		if (string.IsNullOrWhiteSpace(xmlDevice.Extensions.OEMDeviceExtension?.VinNumber))
		{
			throw new ArgumentException("XmlDevice does not contain a VIN");
		}
		RestRequest request = new RestRequest("/Rce/ProtobufApi/UnitService/AssociateVehicle", Method.Post);
		request.AddJsonBody(new AssociateVehicleRequest(new BasicUnitInfo(_unitId), xmlDevice.Extensions.OEMDeviceExtension.VinNumber));
		await _restClient.ExecuteAsync(request);
	}

	public async Task<string> GetMapCareSubscriptionAsync()
	{
		RestRequest request = new RestRequest("/Rce/ProtobufApi/UnitService/GetSubscriptionInfo", Method.Post);
		request.AddJsonBody(new BasicUnitServiceRequest(new BasicUnitInfo(_unitId)));
		return (await _restClient.ExecuteAsync(request)).Content;
	}

	public async Task<UnitManual[]> GetUnitManualsAsync()
	{
		byte[] cacheKey = BitConverter.GetBytes(_unitId.Id);
		UnitManualsResponse unitManualsResponse = _cache.Get<UnitManualsResponse>(cacheKey, TimeSpan.FromDays(14.0));
		if ((object)unitManualsResponse != null)
		{
			return unitManualsResponse.Manuals;
		}
		RestRequest request = new RestRequest("/Rce/ProtobufApi/UnitService/GetUnitManuals", Method.Post);
		request.AddJsonBody(new BasicUnitServiceRequest(new BasicUnitInfo(_unitId)));
		UnitManualsResponse unitManualsResponse2 = default(UnitManualsResponse);
		try
		{
			RestResponse<UnitManualsResponse> restResponse = await _restClient.ExecuteAsync<UnitManualsResponse>(request);
			_cache.Set(cacheKey, restResponse);
			return restResponse.Data.Manuals;
		}
		catch when (((Func<bool>)delegate
		{
			// Could not convert BlockContainer to single expression
			unitManualsResponse2 = _cache.Get<UnitManualsResponse>(cacheKey, TimeSpan.FromDays(28.0));
			return (object)unitManualsResponse2 != null;
		}).Invoke())
		{
			return unitManualsResponse2.Manuals;
		}
	}
}
