using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Networking.Omt.Dto.DlcService;
using Networking.Omt.Dto.ShopService;
using RestSharp;

namespace Networking.Omt;

[NetworkingService(typeof(IDlcService))]
internal sealed class DlcService : IDlcService
{
	private readonly OmtRestClient _restClient;

	private readonly UnitId _unitId;

	private readonly ICache _cache;

	private readonly CustomerGuid? _customerGuid;

	public DlcService(OmtRestClient restClient, UnitId unitId, ICache cache, CustomerGuid? customerGuid = null)
	{
		_restClient = restClient;
		_unitId = unitId;
		_cache = cache;
		_customerGuid = customerGuid;
	}

	public async Task<DlcProductsUpdatesResponse> GetProductDownloadDetailsAsync(string locale, params string[] partNumbers)
	{
		if (partNumbers.Length == 0)
		{
			return new DlcProductsUpdatesResponse(Array.Empty<SafetyCameraUpdateDetails>(), Array.Empty<DownloadableContent>());
		}
		partNumbers = (from i in partNumbers.Distinct()
			orderby i
			select i).ToArray();
		RestRequest request = new RestRequest(string.Format("Rce/ProtobufApi/Units/{0}/products/{1}/download/{2}", _unitId.Id, string.Join(",", partNumbers), locale));
		return (await _restClient.ExecuteAsync<DlcProductsUpdatesResponse>(request)).Data;
	}

	public async Task<ProductDetails[]> GetCompatibleProductsAsync(uint unitId, string locale, string[] partNumbers)
	{
		if (partNumbers.Length == 0)
		{
			return Array.Empty<ProductDetails>();
		}
		partNumbers = (from i in partNumbers.Distinct()
			orderby i
			select i).ToArray();
		RestRequest request = new RestRequest(string.Format("/Rce/ProtobufApi/products/{0}/units/{1}/{2}", string.Join(",", partNumbers), unitId, locale));
		return (await _restClient.ExecuteAsync<DlcProductsDetailsResponse>(request)).Data.Products;
	}

	public async Task<RestResponse> GetCompatibleProductsAsync(string locale, string productType)
	{
		RestRequest request = new RestRequest($"/Rce/ProtobufApi/Units/{_unitId.Id}/Products/Compatible/{locale}/{productType}");
		return await _restClient.ExecuteAsync(request);
	}

	public async Task<DlcProductsStatusResponse> GetAssociatedDlcProductsStatusAsync(uint unitId, string locale)
	{
		RestRequest request = new RestRequest($"/Rce/ProtobufApi/Units/{unitId}/Products/Associated/{locale}");
		return (await _restClient.ExecuteAsync<DlcProductsStatusResponse>(request)).Data;
	}

	public async Task<SafetyCameraUpdateDetails[]> GetSafetyCameraUpdatesAsync(string locale, bool skipCache = false)
	{
		string text = $"/Rce/ProtobufApi/Units/{_unitId.Id}/products/updates/{locale}";
		byte[] cacheKey = Hash.GetSha1(text);
		if (!skipCache)
		{
			DlcProductsUpdatesResponse dlcProductsUpdatesResponse = _cache.Get<DlcProductsUpdatesResponse>(cacheKey, TimeSpan.FromDays(1.0));
			if ((object)dlcProductsUpdatesResponse != null)
			{
				return dlcProductsUpdatesResponse.SafetyCameraUpdateDetails;
			}
		}
		RestRequest request = new RestRequest(text);
		RestResponse<DlcProductsUpdatesResponse> restResponse = await _restClient.ExecuteAsync<DlcProductsUpdatesResponse>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data.SafetyCameraUpdateDetails;
	}

	public async Task<ActivateHuntViewDlcResponse> ActivateHuntViewDlcAsync(string huntviewPartNumber, string[] contentPartNumbers, string locale)
	{
		RestRequest request = new RestRequest($"Rce/ProtobufApi/units/{_unitId.Id}/huntview/{huntviewPartNumber}/activate/{locale}", Method.Post);
		request.AddJsonBody(new ActivateHuntViewDlcRequest
		{
			ContentPartNumbers = contentPartNumbers
		});
		return (await _restClient.ExecuteAsync<ActivateHuntViewDlcResponse>(request)).Data;
	}

	public async Task<SafetyCamerasActivationResponse> ActivateSafetyCamerasAsync(string downloadPartNumber, int regionId, string locale)
	{
		RestRequest request = new RestRequest($"/Rce/ProtobufApi/units/{_unitId.Id}/safetycameras/{downloadPartNumber}/activate/{locale}", Method.Post);
		request.AddJsonBody(new
		{
			RegionId = regionId,
			DownloadDateTimeUtc = DateTime.UtcNow
		});
		return (await _restClient.ExecuteAsync<SafetyCamerasActivationResponse>(request)).Data;
	}

	public async Task<UnlockInfo[]> ActivateProductAsync(string downloadPartNumber, string[] partNumbers, string locale)
	{
		RestRequest request = new RestRequest($"Rce/ProtobufApi/units/{_unitId.Id}/product/{downloadPartNumber}/activate/{locale}", Method.Post);
		request.AddJsonBody(new
		{
			ContentPartNumbers = partNumbers
		});
		return (await _restClient.ExecuteAsync<ProductActivationResponse>(request)).Data.UnlockInfos;
	}

	public async Task<string[]> GetTrafficUnlockCodesAsync(string partNumber, bool skipCache)
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		string text = $"Rce/ProtobufApi/units/{_unitId.Id}/trafficProducts/{partNumber}/unlocks?localeCode={CultureInfo.CurrentCulture.Name}&customerGuid={_customerGuid}";
		byte[] cacheKey = Hash.GetSha1(text);
		if (!skipCache)
		{
			TrafficUnlocksResponse trafficUnlocksResponse = _cache.Get<TrafficUnlocksResponse>(cacheKey, TimeSpan.MaxValue);
			if (trafficUnlocksResponse != null)
			{
				return trafficUnlocksResponse.Unlocks;
			}
		}
		RestRequest request = new RestRequest(text);
		RestResponse<TrafficUnlocksResponse> restResponse = await _restClient.ExecuteAsync<TrafficUnlocksResponse>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data.Unlocks;
	}
}
