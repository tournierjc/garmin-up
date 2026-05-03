using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.InternalOmt.Dto;

namespace Networking.InternalOmt;

public interface IUnits
{
	Task<GarminUnit> GetUnitAsync();

	Task<List<LifetimeMapUpdate>> GetLmusAsync();

	Task<List<MapCareSubscription>> GetMapCareSubscriptionsAsync();

	Task<List<ProductKey>> GetProductKeysAsync();

	Task<List<Unlock>> GetUnlocksAsync();

	Task AddLmuAsync(string productGroupCode);

	Task<ProductKey> AddProductKeyAsync(string productGroupCode);

	Task<Unlock> AddUnlockAsync(string regionPartNumber, bool? isInitial = null);

	Task ResetUnitAsync();

	Task DeleteUnlockAsync(string unlockCode, string regionPartNumber);
}
