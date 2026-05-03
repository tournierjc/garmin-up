using System.Collections.Generic;

namespace Networking.Omt.Dto.MapUpdateService;

public class PreloadedMapUpdatesResponse
{
	public AutoCheckSettings? AutoCheckSettings { get; set; }

	public List<MapUpdateInfo>? MapUpdates { get; set; }

	public List<PurchasableProduct>? PurchasableProducts { get; set; }
}
