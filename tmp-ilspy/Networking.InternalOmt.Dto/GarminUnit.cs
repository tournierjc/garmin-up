using System.Collections.Generic;

namespace Networking.InternalOmt.Dto;

public record GarminUnit(string Description, List<Unlock> InactiveUnlocks, List<LifetimeMapUpdate> LifetimeMapUpdates, List<MapCareSubscription> MapCareSubscriptions, List<ProductKey> ProductKeys, uint UnitId, List<Unlock> Unlocks);
