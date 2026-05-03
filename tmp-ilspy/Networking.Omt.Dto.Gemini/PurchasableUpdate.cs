using Networking.Omt.Dto.Common;

namespace Networking.Omt.Dto.Gemini;

public class PurchasableUpdate
{
	public string? PartNumber { get; set; }

	public string? RegionPartNumber { get; set; }

	public string? DisplayName { get; set; }

	public Release? Release { get; set; }
}
