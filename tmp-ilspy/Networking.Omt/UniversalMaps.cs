using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Networking.Omt.Dto.Common;
using Networking.Omt.Dto.UniversalMaps;
using RestSharp;

namespace Networking.Omt;

internal sealed class UniversalMaps : IUniversalMaps
{
	private readonly OmtRestClient _restClient;

	private readonly ICache _cache;

	public UniversalMaps(OmtRestClient restClient, ICache cache)
	{
		_restClient = restClient;
		_cache = cache;
	}

	public async Task<ActivateUpdatesResponse> ActivateAsync(ActivateUpdatesRequest request)
	{
		RestRequest request2 = new RestRequest("/api/maps/universal/activate", Method.Post);
		request2.AddJsonBody(request);
		return (await _restClient.ExecuteAsync<ActivateUpdatesResponse>(request2)).Data;
	}

	public async Task<GetUpdatesResponse> GetReinstallsAsync(GetUpdatesRequest request)
	{
		string text = "/api/maps/universal/reinstall";
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData(text);
		sha.AppendData(request.GarminDeviceXml);
		sha.AppendData(request.Options.ExtractionMode);
		sha.AppendData(request.Options.GroupingMode);
		byte[] cacheKey = sha.GetHashAndReset();
		GetUpdatesResponse getUpdatesResponse = _cache.Get<GetUpdatesResponse>(cacheKey, TimeSpan.FromHours(1.0));
		if (getUpdatesResponse != null)
		{
			return AddCleanupRulesToMaps(getUpdatesResponse);
		}
		RestRequest request2 = new RestRequest(text, Method.Post);
		request2.AddJsonBody(request);
		RestResponse<GetUpdatesResponse> restResponse = await _restClient.ExecuteAsync<GetUpdatesResponse>(request2);
		_cache.Set(cacheKey, restResponse);
		return AddCleanupRulesToMaps(restResponse.Data);
	}

	public async Task<GetUpdatesResponse> GetUpdatesAsync(GetUpdatesRequest request)
	{
		string text = "/api/maps/universal/update";
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData(text);
		sha.AppendData(request.GarminDeviceXml);
		sha.AppendData(request.Options.ExtractionMode);
		sha.AppendData(request.Options.GroupingMode);
		byte[] cacheKey = sha.GetHashAndReset();
		GetUpdatesResponse getUpdatesResponse = _cache.Get<GetUpdatesResponse>(cacheKey, TimeSpan.FromHours(1.0));
		if (getUpdatesResponse != null)
		{
			return AddCleanupRulesToMaps(getUpdatesResponse);
		}
		RestRequest request2 = new RestRequest(text, Method.Post);
		request2.AddJsonBody(request);
		RestResponse<GetUpdatesResponse> restResponse = await _restClient.ExecuteAsync<GetUpdatesResponse>(request2);
		_cache.Set(cacheKey, restResponse);
		return AddCleanupRulesToMaps(restResponse.Data);
	}

	private GetUpdatesResponse AddCleanupRulesToMaps(GetUpdatesResponse gur)
	{
		return new GetUpdatesResponse
		{
			DirectoriesToRemove = gur.DirectoriesToRemove,
			FilesToRemove = gur.FilesToRemove,
			Maps = gur.Maps.Select((UniversalMap i) => i with
			{
				FileCleanupRules = gur.DirectoriesToRemove
			}).ToArray(),
			BundledMaps = gur.BundledMaps
		};
	}
}
