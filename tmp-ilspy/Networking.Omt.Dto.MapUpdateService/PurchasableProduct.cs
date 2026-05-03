using System.Collections.Generic;

namespace Networking.Omt.Dto.MapUpdateService;

public class PurchasableProduct
{
	public string? PartNumber { get; set; }

	public List<string>? ProductGroups { get; set; }
}
