using System;
using Networking.Auth;
using RestSharp;

namespace Networking;

internal interface ICache
{
	T? Get<T>(byte[] cacheKey, TimeSpan cacheLength) where T : class;

	void Set<T>(byte[] cacheKey, RestResponse<T> response) where T : class;

	void Set<T>(byte[] cacheKey, T obj) where T : class;

	DeviceAuth Get(UnitId unitId);

	void Set(DeviceAuth auth);

	AccountAuth Get(CustomerGuid customerGuid);

	void Set(AccountAuth auth);

	void DeleteAuth(UnitId unitId);

	void DeleteAuth(CustomerGuid customerGuid);
}
