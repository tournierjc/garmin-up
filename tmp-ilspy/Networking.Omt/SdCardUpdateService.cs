using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Networking.Omt.Dto.SdCardUpdateService;
using RestSharp;

namespace Networking.Omt;

internal sealed class SdCardUpdateService : ISdCardUpdateService
{
	private record SdCardRegistration(Guid CustomerGuid);

	private readonly OmtRestClient _restClient;

	private readonly ICache _cache;

	private readonly CustomerGuid? _customerGuid;

	public SdCardUpdateService(OmtRestClient restClient, ICache cache, CustomerGuid? customerGuid = null)
	{
		_restClient = restClient;
		_cache = cache;
		_customerGuid = customerGuid;
	}

	public async Task<ActivateSdCardUpdatesResponse> ActivateAsync(byte[] signedSdCardBytes, Identifier identifier)
	{
		RestRequest request = new RestRequest("/api/maps/sdcard/activate", Method.Post);
		request.AddJsonBody(new
		{
			signedSdCardBytes = signedSdCardBytes,
			installedMap = identifier
		});
		return (await _restClient.ExecuteAsync<ActivateSdCardUpdatesResponse>(request)).Data;
	}

	public async Task RegisterSdCardAsync(Guid sdCardGuid)
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData("/api/maps/sdcard/register");
		sha.AppendData(sdCardGuid);
		byte[] cacheKey = sha.GetHashAndReset();
		SdCardRegistration sdCardRegistration = _cache.Get<SdCardRegistration>(cacheKey, TimeSpan.FromHours(1.0));
		if ((object)sdCardRegistration == null || !(sdCardRegistration.CustomerGuid == _customerGuid.Guid))
		{
			RestRequest request = new RestRequest("/api/maps/sdcard/register", Method.Post);
			request.AddJsonBody(new
			{
				sdCardGuid = sdCardGuid,
				customerGuid = _customerGuid.Guid
			});
			await _restClient.ExecuteAsync(request);
			_cache.Set(cacheKey, new SdCardRegistration(_customerGuid.Guid));
		}
	}

	public async Task<DisplayInfoResponse> GetDisplayInfoAsync(byte[] signedSdCardBytes)
	{
		using SHA1 sha = SHA1.Create();
		byte[] cacheKey = sha.ComputeHash(signedSdCardBytes);
		DisplayInfoResponse displayInfoResponse = _cache.Get<DisplayInfoResponse>(cacheKey, TimeSpan.FromDays(3.0));
		if ((object)displayInfoResponse != null)
		{
			return displayInfoResponse;
		}
		RestRequest request = new RestRequest("/api/maps/sdcard/displayinfo", Method.Post);
		request.AddJsonBody(new { signedSdCardBytes });
		RestResponse<DisplayInfoResponse> restResponse = await _restClient.ExecuteAsync<DisplayInfoResponse>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data;
	}

	public async Task<SdCardUpdateResponse> GetReinstallAsync(byte[] signedSdCardBytes, bool useCache = true)
	{
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData("/api/maps/sdcard/reinstall");
		sha.AppendData(signedSdCardBytes);
		byte[] cacheKey = sha.GetHashAndReset();
		if (useCache)
		{
			SdCardUpdateResponse sdCardUpdateResponse = _cache.Get<SdCardUpdateResponse>(cacheKey, TimeSpan.FromDays(3.0));
			if ((object)sdCardUpdateResponse != null)
			{
				return sdCardUpdateResponse;
			}
		}
		RestRequest request = new RestRequest("/api/maps/sdcard/reinstall", Method.Post);
		request.AddJsonBody(new { signedSdCardBytes });
		RestResponse<SdCardUpdateResponse> restResponse = await _restClient.ExecuteAsync<SdCardUpdateResponse>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data;
	}

	public async Task<SdCardUpdateResponse> GetUpdateAsync(byte[] signedSdCardBytes, bool useCache = true)
	{
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData("/api/maps/sdcard/update");
		sha.AppendData(signedSdCardBytes);
		if (_customerGuid != null)
		{
			sha.AppendData(_customerGuid.Guid);
		}
		byte[] cacheKey = sha.GetHashAndReset();
		if (useCache)
		{
			SdCardUpdateResponse sdCardUpdateResponse = _cache.Get<SdCardUpdateResponse>(cacheKey, TimeSpan.FromDays(1.0));
			if ((object)sdCardUpdateResponse != null)
			{
				return sdCardUpdateResponse;
			}
		}
		RestRequest request = new RestRequest("/api/maps/sdcard/update", Method.Post);
		request.AddJsonBody(new
		{
			signedSdCardBytes = signedSdCardBytes,
			customerGuid = _customerGuid?.Guid
		});
		RestResponse<SdCardUpdateResponse> restResponse = await _restClient.ExecuteAsync<SdCardUpdateResponse>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data;
	}
}
