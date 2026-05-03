using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Networking.Omt.Dto.ShopService;
using RestSharp;

namespace Networking.Omt;

internal sealed class ShopService : IShopService
{
	private record ConflictingProductsResponse(ProductDetails[] ConflictingProductDetails);

	private record CountriesRequest(uint? UnitId, string[]? PartNumbers);

	private record CountriesResponse(Country[] Countries);

	private record GetOrdersResponse(OrderDetails[] OrderDetails);

	private record InactiveProductsResponse(ProductDetails[] InactiveProductDetails);

	private record PartnerStatusResponse(PartnerStatus PartnerStatus);

	private record RecommendedProductsRequest(long UnitId, string Locale);

	private record RecommendedProductsResponse(RecommendedProduct[]? Products);

	private readonly ILogger<ShopService> _logger;

	private readonly OmtRestClient _restClient;

	private readonly OmtITRestClient _omtITRestClient;

	private readonly ICache _cache;

	private readonly CustomerGuid? _customerGuid;

	public ShopService(ILogger<ShopService> logger, OmtRestClient restClient, OmtITRestClient omtItRestClient, ICache cache, CustomerGuid? customerGuid = null)
	{
		_logger = logger;
		_restClient = restClient;
		_omtITRestClient = omtItRestClient;
		_cache = cache;
		_customerGuid = customerGuid;
	}

	public async Task AssociateOrderToUnitAsync(uint unitId, string orderId)
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		RestRequest request = new RestRequest($"/api/shop/customers/{_customerGuid}/units/{unitId}/order/{orderId}/associate", Method.Post);
		await _omtITRestClient.ExecuteAsync(request);
	}

	public async Task AssociatePurchaseToUnitAsync(uint unitId, string productPartNumber)
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		RestRequest request = new RestRequest($"/api/shop/customers/{_customerGuid}/units/{unitId}/product/{productPartNumber}/associate", Method.Post);
		await _omtITRestClient.ExecuteAsync(request);
	}

	public async Task<ShellOrderResponse> CreateShellOrderAsync(ShellOrderRequest request)
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		ShellOrderRequest obj = request with
		{
			CustomerGuid = _customerGuid.Guid
		};
		RestRequest request2 = new RestRequest($"/api/shop/customers/{_customerGuid.Guid}/order", Method.Post);
		request2.AddJsonBody(obj);
		return (await _omtITRestClient.ExecuteAsync<ShellOrderResponse>(request2)).Data;
	}

	public async Task<ProductDetails[]> GetCustomerConflictingProductsAsync(string productPartNumber)
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		RestRequest request = new RestRequest($"/api/shop/customers/{_customerGuid}/products/{productPartNumber}/conflicting");
		return (await _omtITRestClient.ExecuteAsync<ConflictingProductsResponse>(request)).Data.ConflictingProductDetails;
	}

	public async Task<ProductDetails[]> GetCustomerInactiveProductsAsync()
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		RestRequest request = new RestRequest($"/api/shop/customers/{_customerGuid}/products/inactive");
		return (await _omtITRestClient.ExecuteAsync<InactiveProductsResponse>(request)).Data.InactiveProductDetails;
	}

	public async Task<PartnerStatus> GetCustomerPartnerStatusAsync()
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		RestRequest request = new RestRequest($"/api/shop/customers/{_customerGuid}/partner/status");
		return (await _omtITRestClient.ExecuteAsync<PartnerStatusResponse>(request)).Data.PartnerStatus;
	}

	public async Task<OrderDetails> GetOrderDetailsAsync(string orderId)
	{
		RestRequest request = new RestRequest("/api/shop/orders/" + orderId);
		return (await _omtITRestClient.ExecuteAsync<OrderDetails>(request)).Data;
	}

	public async Task<OrderDetails[]> GetOrdersAsync()
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		RestRequest request = new RestRequest($"/api/shop/customers/{_customerGuid}/orders");
		return (await _omtITRestClient.ExecuteAsync<GetOrdersResponse>(request)).Data.OrderDetails;
	}

	public async Task<ProductDetails> GetProductDetailsAsync(string productPartNumber)
	{
		string text = "/api/shop/products/" + productPartNumber + "/details";
		using IncrementalHash sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
		sha.AppendData(text);
		byte[] cacheKey = sha.GetHashAndReset();
		ProductDetails productDetails = _cache.Get<ProductDetails>(cacheKey, TimeSpan.FromHours(1.0));
		if ((object)productDetails != null)
		{
			return productDetails;
		}
		RestRequest request = new RestRequest(text);
		RestResponse<ProductDetails> restResponse = await _restClient.ExecuteAsync<ProductDetails>(request);
		_cache.Set(cacheKey, restResponse);
		return restResponse.Data;
	}

	public async Task<PricedShoppingCartLine?> GetProductPriceAsync(ProductPriceRequest request)
	{
		string resource = "/api/shop/products/" + request.Product.PartNumber + "/price";
		if (_customerGuid != null)
		{
			request = request with
			{
				CustomerGuid = _customerGuid.Guid
			};
			resource = $"/api/shop/customers/{_customerGuid.Guid}/products/{request.Product.PartNumber}/price";
		}
		RestRequest request2 = new RestRequest(resource, Method.Post);
		request2.AddJsonBody(request);
		try
		{
			RestResponse<PricedShoppingCartLine> restResponse = ((!(_customerGuid != null)) ? (await _restClient.ExecuteAsync<PricedShoppingCartLine>(request2)) : (await _omtITRestClient.ExecuteAsync<PricedShoppingCartLine>(request2)));
			return restResponse.Data;
		}
		catch (RestRequestException ex) when (ex.Response.StatusCode == HttpStatusCode.NotFound)
		{
			IList<string> values = request.PromotionCodes ?? new List<string>();
			_logger.LogWarning("No pricing details available for " + request.Product.PartNumber + " with promo codes: [ " + string.Join(", ", values) + " ].");
			return null;
		}
	}

	public async Task<ScrIdsForOrderResponse> GetScrIdsForOrderAsync(string orderId)
	{
		RestRequest request = new RestRequest("/api/shop/orders/" + orderId + "/scrids");
		return (await _omtITRestClient.ExecuteAsync<ScrIdsForOrderResponse>(request)).Data;
	}

	public async Task<Country[]> GetWhiteLabelCheckoutCountriesAsync(uint? unitId, string[]? partNumbers)
	{
		RestRequest request = new RestRequest("/api/shop/countries", Method.Post);
		request.AddJsonBody(new CountriesRequest(unitId, partNumbers));
		return (await _restClient.ExecuteAsync<CountriesResponse>(request)).Data.Countries;
	}

	public async Task OptOutOfPartnerAccountAsync()
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		RestRequest request = new RestRequest($"/api/shop/customers/{_customerGuid}/partner/optout", Method.Post);
		await _omtITRestClient.ExecuteAsync(request);
	}

	public async Task<CustomerDisplayInfo> GetCustomerDisplayInfoAsync()
	{
		if (_customerGuid == null)
		{
			throw new ITAuthorizationException();
		}
		RestRequest request = new RestRequest($"/api/shop/customers/{_customerGuid}/displayinfo");
		return (await _omtITRestClient.ExecuteAsync<CustomerDisplayInfo>(request)).Data;
	}

	public async Task<RecommendedProduct[]?> GetRecommendedProductsAsync(uint unitId, string locale)
	{
		RestRequest request = new RestRequest("/Rce/ProtobufApi/ShopService/GetRecommendedProducts", Method.Post);
		request.AddJsonBody(new RecommendedProductsRequest(unitId, locale));
		return (await _restClient.ExecuteAsync<RecommendedProductsResponse>(request)).Data?.Products;
	}
}
