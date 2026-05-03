using System.Threading.Tasks;
using Networking.Omt.Dto.MarineChartService;

namespace Networking.Omt;

public interface IMarineChartService
{
	Task<ChartsForCustomerResponse> GetChartsForCustomerAsync(string locale, bool useCache = true);

	Task<ChartManifestResponse> GetChartManifestAsync(string locale, string scrId);

	Task<ChartUnlocksResponse> GetChartUnlocksAsync(string scrId);

	Task<GmaContent> GetGmaContentAsync(string locale, string gma);

	Task<Chart> RedeemChartAsync(string partNumber);
}
