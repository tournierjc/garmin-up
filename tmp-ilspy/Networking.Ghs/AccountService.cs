using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Networking.Auth;
using Networking.Ghs.Dto;
using RestSharp;

namespace Networking.Ghs;

internal sealed class AccountService : IAccountService
{
	private readonly GhsRestClient _restClient;

	private readonly IAuthDataProvider _authDataProvider;

	private readonly UnitId _unitId;

	private readonly ICache _cache;

	public AccountService(GhsRestClient restClient, IAuthDataProvider authDataProvider, UnitId unitId, ICache cache)
	{
		_restClient = restClient;
		_authDataProvider = authDataProvider;
		_unitId = unitId;
		_cache = cache;
	}

	public async Task<AccountStatus> GetAccountStatusAsync()
	{
		ConnectAuth connectAuth = _authDataProvider.GetConnectAuth(_unitId) ?? throw new ConnectAuthorizationException();
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData(_unitId.Id);
		sha.AppendData(connectAuth.Token);
		byte[] cacheKey = sha.GetHashAndReset();
		AccountStatus accountStatus = _cache.Get<AccountStatus>(cacheKey, TimeSpan.FromHours(1.0));
		if ((object)accountStatus != null)
		{
			return accountStatus;
		}
		AccountStatus accountStatus2 = _cache.Get<AccountStatus>(cacheKey, TimeSpan.FromDays(7.0));
		if ((object)accountStatus2 != null && accountStatus2.Exists)
		{
			return accountStatus2;
		}
		RestRequest request = new RestRequest("/ghs/account/v1/account");
		RestResponse<AccountStatus> restResponse = await _restClient.ExecuteAsync<AccountStatus>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data;
	}
}
