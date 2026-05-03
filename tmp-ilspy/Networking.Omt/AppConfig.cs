using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Networking.Omt.Dto.AppConfig;
using RestSharp;

namespace Networking.Omt;

internal sealed class AppConfig : IAppConfig
{
	private static readonly string s_appName = Path.GetFileNameWithoutExtension(AppDomain.CurrentDomain.FriendlyName);

	private readonly OmtRestClient _restClient;

	private readonly ICache _cache;

	public AppConfig(OmtRestClient restClient, ICache cache)
	{
		_restClient = restClient;
		_cache = cache;
	}

	public async Task<AppRule[]> GetAppConfigAsync()
	{
		string text = "/api/app-config/applications/" + s_appName + "/versions/0/operatingsystems/windows/versions/0";
		RestRequest request = new RestRequest(text);
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData(text);
		byte[] cacheKey = sha.GetHashAndReset();
		AppRule[] array = _cache.Get<AppRule[]>(cacheKey, TimeSpan.FromDays(1.0));
		if (array != null)
		{
			return array;
		}
		RestResponse<AppRule[]> restResponse = await _restClient.ExecuteAsync<AppRule[]>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data;
	}
}
