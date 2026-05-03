using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Networking.Omt.Dto.MarineChartService;
using RestSharp;

namespace Networking.Omt;

internal sealed class MarineChartService : IMarineChartService
{
	private record ChartsForCustomerRequest(Guid CustomerGuid, string Locale);

	private record ChartManifestRequest(Guid CustomerGuid, string Locale, string SellableChartRegionId);

	private record ChartUnlocksRequest(Guid CustomerGuid, string SellableChartRegionId);

	private record GetGmaContentRequest(string Locale, string Gma);

	private record GetGmaContentResponse(GmaContent Product);

	private record RedeemChartRequest(Guid CustomerGuid, string DownloadPartNumber);

	private record RedeemChartResponse(Chart RedeemedChart);

	private readonly OmtRestClient _restClient;

	private readonly ICache _cache;

	private readonly CustomerGuid? _customerGuid;

	public MarineChartService(OmtRestClient restClient, ICache cache, CustomerGuid? customerGuid = null)
	{
		_restClient = restClient;
		_cache = cache;
		_customerGuid = customerGuid;
	}

	public async Task<ChartsForCustomerResponse> GetChartsForCustomerAsync(string locale, bool useCache)
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		ChartsForCustomerRequest chartsForCustomerRequest = new ChartsForCustomerRequest(_customerGuid.Guid, locale);
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData("MapUpdateService/Marine/ChartsForCustomer");
		sha.AppendData(chartsForCustomerRequest.ToString());
		byte[] cacheKey = sha.GetHashAndReset();
		if (useCache)
		{
			ChartsForCustomerResponse chartsForCustomerResponse = _cache.Get<ChartsForCustomerResponse>(cacheKey, TimeSpan.FromHours(1.0));
			if ((object)chartsForCustomerResponse != null)
			{
				return chartsForCustomerResponse;
			}
		}
		RestRequest request = new RestRequest("MapUpdateService/Marine/ChartsForCustomer", Method.Post);
		request.AddJsonBody(chartsForCustomerRequest);
		RestResponse<ChartsForCustomerResponse> restResponse = await _restClient.ExecuteAsync<ChartsForCustomerResponse>(request);
		restResponse.Data = restResponse.Data with
		{
			UtcTimeStamp = DateTime.UtcNow
		};
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data;
	}

	public async Task<ChartManifestResponse> GetChartManifestAsync(string locale, string scrId)
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		ChartManifestRequest obj = new ChartManifestRequest(_customerGuid.Guid, locale, scrId);
		RestRequest request = new RestRequest("MapUpdateService/Marine/DownloadManifest", Method.Post);
		request.AddJsonBody(obj);
		return (await _restClient.ExecuteAsync<ChartManifestResponse>(request)).Data;
	}

	public async Task<ChartUnlocksResponse> GetChartUnlocksAsync(string scrId)
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		ChartUnlocksRequest obj = new ChartUnlocksRequest(_customerGuid.Guid, scrId);
		RestRequest request = new RestRequest("MapUpdateService/Marine/UnlocksForCustomer", Method.Post);
		request.AddJsonBody(obj);
		return (await _restClient.ExecuteAsync<ChartUnlocksResponse>(request)).Data;
	}

	public async Task<GmaContent> GetGmaContentAsync(string locale, string gma)
	{
		GetGmaContentRequest getGmaContentRequest = new GetGmaContentRequest(locale, gma);
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData("MapUpdateService/Marine/GmaProducts");
		sha.AppendData(getGmaContentRequest.ToString());
		byte[] cacheKey = sha.GetHashAndReset();
		GetGmaContentResponse getGmaContentResponse = _cache.Get<GetGmaContentResponse>(cacheKey, TimeSpan.FromHours(1.0));
		if ((object)getGmaContentResponse != null)
		{
			return getGmaContentResponse.Product with
			{
				Gma = gma
			};
		}
		RestRequest request = new RestRequest("MapUpdateService/Marine/GmaProducts", Method.Post);
		request.AddJsonBody(getGmaContentRequest);
		RestResponse<GetGmaContentResponse> restResponse = await _restClient.ExecuteAsync<GetGmaContentResponse>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data.Product with
		{
			Gma = gma
		};
	}

	public async Task<Chart> RedeemChartAsync(string partNumber)
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		RedeemChartRequest obj = new RedeemChartRequest(_customerGuid.Guid, partNumber);
		RestRequest request = new RestRequest("MapUpdateService/Marine/RedeemFreeChartUpdate", Method.Post);
		request.AddJsonBody(obj);
		return (await _restClient.ExecuteAsync<RedeemChartResponse>(request)).Data.RedeemedChart;
	}
}
