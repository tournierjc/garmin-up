using System.Collections.Generic;
using System.Threading.Tasks;
using Networking.Omt.Dto.MapUpdateService;

namespace Networking.Omt;

public interface IMapUpdateService
{
	Task<PreloadedMapUpdatesResponse> GetPreloadedMapUpdatesAsync(RequestSource requestSource = RequestSource.Foreground, bool skipCache = false);

	Task<DownloadedDetailsResponse> GetDownloadDetailsAsync(string partNumber, bool skipCache = false);

	Task<string> ActivateMapUpdate(MapUpdateInfo updateInfo, List<string> partNumbersToInstall);
}
