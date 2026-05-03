using Networking.Omt.Dto.ShopService;

namespace Networking.Omt.Dto.DlcService;

internal class DlcProductsDetailsResponse
{
	public required ProductDetails[] Products { get; init; }
}
