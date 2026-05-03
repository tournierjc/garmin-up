using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DeviceXmlUtil;
using Networking.Omt.Dto.Common;
using Networking.Omt.Dto.MapUpdateService;
using Networking.Omt.Dto.UnitService;
using RestSharp;

namespace Networking.Omt;

internal sealed class MapUpdateService : IMapUpdateService
{
	private record PreloadedMapUpdatesRequest(BasicUnitInfo BasicUnitInfo, bool IsUserInteractive)
	{
		public object ClientInfo => new
		{
			LocaleCode = CultureInfo.CurrentCulture.Name
		};
	}

	private record PreloadedMapWithDeviceXmlUpdatesRequest(BasicUnitInfo BasicUnitInfo, bool IsUserInteractive, string DeviceXml)
	{
		public object ClientInfo => new
		{
			LocaleCode = CultureInfo.CurrentCulture.Name
		};
	}

	private record DownloadDetailsRequest(FullUnitInfo FullUnitInfo, string PartNumber)
	{
		public object ClientInfo => new
		{
			LocaleCode = CultureInfo.CurrentCulture.Name
		};
	}

	private record ActivateMapUpdateRequest(FullUnitInfo FullUnitInfo, MapUpdateInfo UpdateInfo, List<string> PartNumbersToInstall)
	{
		public object ClientInfo => new
		{
			LocaleCode = CultureInfo.CurrentCulture.Name
		};
	}

	private readonly OmtRestClient _restClient;

	private readonly ICache _cache;

	private readonly Func<XmlDevice> _xmlDeviceFunc;

	private readonly Func<UnitId, IUnitService> _unitServiceFunc;

	public MapUpdateService(OmtRestClient restClient, ICache cache, Func<XmlDevice> xmlDeviceFunc, Func<UnitId, IUnitService> unitServiceFunc)
	{
		_restClient = restClient;
		_cache = cache;
		_xmlDeviceFunc = xmlDeviceFunc;
		_unitServiceFunc = unitServiceFunc;
	}

	public async Task<PreloadedMapUpdatesResponse> GetPreloadedMapUpdatesAsync(RequestSource requestSource, bool skipCache)
	{
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		sha.AppendData(_xmlDeviceFunc().ToString());
		byte[] cacheKey = sha.GetHashAndReset();
		TimeSpan cacheLength = requestSource switch
		{
			RequestSource.Interaction => TimeSpan.FromHours(1.0), 
			RequestSource.Foreground => TimeSpan.FromDays(3.0), 
			RequestSource.Background => TimeSpan.FromDays(7.0), 
			_ => throw new ArgumentOutOfRangeException(), 
		};
		if (!skipCache)
		{
			PreloadedMapUpdatesResponse preloadedMapUpdatesResponse = _cache.Get<PreloadedMapUpdatesResponse>(cacheKey, cacheLength);
			if (preloadedMapUpdatesResponse != null)
			{
				return preloadedMapUpdatesResponse;
			}
		}
		UnitDisplayInfo unitDisplayInfo = await _unitServiceFunc(new UnitId(_xmlDeviceFunc().Id)).GetUnitDisplayInfoAsync(_xmlDeviceFunc().Model.PartNumber);
		RestRequest request = new RestRequest("/Rce/ProtobufApi/MapUpdateService/GetPreloadedMapUpdates", Method.Post);
		if ((unitDisplayInfo.ApplicationBehaviors?.ContainsKey("GetPreloadedMapUpdatesIncludeFullXML") ?? false) && Convert.ToBoolean(unitDisplayInfo.ApplicationBehaviors?["GetPreloadedMapUpdatesIncludeFullXML"]))
		{
			request.AddJsonBody(new PreloadedMapWithDeviceXmlUpdatesRequest(new BasicUnitInfo(_xmlDeviceFunc()), requestSource == RequestSource.Interaction, _xmlDeviceFunc().ToString()));
		}
		else
		{
			request.AddJsonBody(new PreloadedMapUpdatesRequest(new BasicUnitInfo(_xmlDeviceFunc()), requestSource == RequestSource.Interaction));
		}
		RestResponse<PreloadedMapUpdatesResponse> restResponse = await _restClient.ExecuteAsync<PreloadedMapUpdatesResponse>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data;
	}

	public async Task<DownloadedDetailsResponse> GetDownloadDetailsAsync(string partNumber, bool skipCache)
	{
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
		sha.AppendData(_xmlDeviceFunc().ToString());
		sha.AppendData(partNumber);
		byte[] cacheKey = sha.GetHashAndReset();
		if (!skipCache)
		{
			DownloadedDetailsResponse downloadedDetailsResponse = _cache.Get<DownloadedDetailsResponse>(cacheKey, TimeSpan.FromHours(1.0));
			if ((object)downloadedDetailsResponse != null)
			{
				return downloadedDetailsResponse;
			}
		}
		RestRequest request = new RestRequest("/Rce/ProtobufApi/MapUpdateService/GetDownloadDetails", Method.Post);
		request.AddJsonBody(new DownloadDetailsRequest(new FullUnitInfo(_xmlDeviceFunc()), partNumber));
		RestResponse<DownloadedDetailsResponse> restResponse = await _restClient.ExecuteAsync<DownloadedDetailsResponse>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data;
	}

	public async Task<string> ActivateMapUpdate(MapUpdateInfo updateInfo, List<string> partNumbersToInstall)
	{
		RestRequest request = new RestRequest("/Rce/ProtobufApi/MapUpdateService/ActivateMapUpdate", Method.Post);
		request.AddJsonBody(new ActivateMapUpdateRequest(new FullUnitInfo(_xmlDeviceFunc()), updateInfo, partNumbersToInstall));
		return (await _restClient.ExecuteAsync(request)).Content;
	}
}
