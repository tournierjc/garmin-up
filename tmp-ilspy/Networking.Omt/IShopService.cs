using System.Threading.Tasks;
using Networking.Omt.Dto.ShopService;

namespace Networking.Omt;

public interface IShopService
{
	Task AssociateOrderToUnitAsync(uint unitId, string orderId);

	Task AssociatePurchaseToUnitAsync(uint unitId, string productPartNumber);

	Task<ShellOrderResponse> CreateShellOrderAsync(ShellOrderRequest request);

	Task<ProductDetails[]> GetCustomerConflictingProductsAsync(string productPartNumber);

	Task<ProductDetails[]> GetCustomerInactiveProductsAsync();

	Task<PartnerStatus> GetCustomerPartnerStatusAsync();

	Task<OrderDetails> GetOrderDetailsAsync(string orderId);

	Task<OrderDetails[]> GetOrdersAsync();

	Task<ProductDetails> GetProductDetailsAsync(string productPartNumber);

	Task<PricedShoppingCartLine?> GetProductPriceAsync(ProductPriceRequest request);

	Task<ScrIdsForOrderResponse> GetScrIdsForOrderAsync(string orderId);

	Task<Country[]> GetWhiteLabelCheckoutCountriesAsync(uint? unitId, string[]? partNumbers);

	Task OptOutOfPartnerAccountAsync();

	Task<CustomerDisplayInfo> GetCustomerDisplayInfoAsync();

	Task<RecommendedProduct[]?> GetRecommendedProductsAsync(uint unitId, string locale);
}
