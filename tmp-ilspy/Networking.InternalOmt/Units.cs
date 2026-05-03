using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.InternalOmt.Dto;
using RestSharp;

namespace Networking.InternalOmt;

internal sealed class Units : IUnits
{
	private readonly InternalOmtRestClient _restClient;

	private readonly uint _unitId;

	public Units(InternalOmtRestClient restClient, UnitId unitId)
	{
		_restClient = restClient;
		_unitId = unitId.Id;
	}

	public async Task<GarminUnit> GetUnitAsync()
	{
		RestRequest request = new RestRequest($"/rce/internal/units/{_unitId}");
		return (await _restClient.ExecuteAsync<GarminUnit>(request)).Data;
	}

	public async Task<List<LifetimeMapUpdate>> GetLmusAsync()
	{
		RestRequest request = new RestRequest($"/rce/internal/units/{_unitId}/lmus");
		return (await _restClient.ExecuteAsync<List<LifetimeMapUpdate>>(request)).Data;
	}

	public async Task<List<MapCareSubscription>> GetMapCareSubscriptionsAsync()
	{
		RestRequest request = new RestRequest($"/rce/internal/units/{_unitId}/mapcaresubscriptions");
		return (await _restClient.ExecuteAsync<List<MapCareSubscription>>(request)).Data;
	}

	public async Task<List<ProductKey>> GetProductKeysAsync()
	{
		RestRequest request = new RestRequest($"/rce/internal/units/{_unitId}/productkeys");
		return (await _restClient.ExecuteAsync<List<ProductKey>>(request)).Data;
	}

	public async Task<List<Unlock>> GetUnlocksAsync()
	{
		RestRequest request = new RestRequest($"/rce/internal/units/{_unitId}/unlocks");
		return (await _restClient.ExecuteAsync<List<Unlock>>(request)).Data;
	}

	public async Task AddLmuAsync(string productGroupCode)
	{
		RestRequest request = new RestRequest($"/rce/internal/units/{_unitId}/lmus/{productGroupCode}", Method.Post);
		await _restClient.ExecuteAsync(request);
	}

	public async Task<ProductKey> AddProductKeyAsync(string productGroupCode)
	{
		RestRequest request = new RestRequest($"/rce/internal/units/{_unitId}/productkeys/{productGroupCode}", Method.Post);
		return (await _restClient.ExecuteAsync<ProductKey>(request)).Data;
	}

	public async Task<Unlock> AddUnlockAsync(string regionPartNumber, bool? isInitial)
	{
		RestRequest request = new RestRequest($"/rce/internal/units/{_unitId}/unlocks/{regionPartNumber}", Method.Post);
		if (isInitial.HasValue)
		{
			request.AddParameter("initial", isInitial.Value);
		}
		return (await _restClient.ExecuteAsync<Unlock>(request)).Data;
	}

	public async Task ResetUnitAsync()
	{
		RestRequest request = new RestRequest($"/rce/internal/units/{_unitId}", Method.Delete);
		await _restClient.ExecuteAsync(request);
	}

	public async Task DeleteUnlockAsync(string unlockCode, string regionPartNumber)
	{
		RestRequest request = new RestRequest($"/rce/internal/units/{_unitId}/unlocks/{unlockCode}/region/{regionPartNumber}", Method.Delete);
		await _restClient.ExecuteAsync(request);
	}
}
