using System.Threading.Tasks;
using Networking.Omt.Dto.DlcService;
using Networking.Omt.Dto.ShopService;
using RestSharp;

namespace Networking.Omt;

public interface IDlcService
{
	Task<DlcProductsUpdatesResponse> GetProductDownloadDetailsAsync(string locale, params string[] partNumbers);

	Task<ProductDetails[]> GetCompatibleProductsAsync(uint unitId, string locale, string[] partNumbers);

	Task<DlcProductsStatusResponse> GetAssociatedDlcProductsStatusAsync(uint unitId, string locale);

	Task<RestResponse> GetCompatibleProductsAsync(string locale, string productType);

	Task<SafetyCameraUpdateDetails[]> GetSafetyCameraUpdatesAsync(string locale, bool skipCache = false);

	Task<ActivateHuntViewDlcResponse> ActivateHuntViewDlcAsync(string huntviewPartNumber, string[] contentPartNumbers, string locale);

	Task<SafetyCamerasActivationResponse> ActivateSafetyCamerasAsync(string downloadPartNumber, int regionId, string locale);

	Task<UnlockInfo[]> ActivateProductAsync(string downloadPartNumber, string[] partNumbers, string locale);

	Task<string[]> GetTrafficUnlockCodesAsync(string partNumber, bool skipCache = false);
}
