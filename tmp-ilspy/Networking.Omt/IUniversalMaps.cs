using System.Threading.Tasks;
using Networking.Omt.Dto.Common;
using Networking.Omt.Dto.UniversalMaps;

namespace Networking.Omt;

public interface IUniversalMaps
{
	Task<ActivateUpdatesResponse> ActivateAsync(ActivateUpdatesRequest request);

	Task<GetUpdatesResponse> GetReinstallsAsync(GetUpdatesRequest request);

	Task<GetUpdatesResponse> GetUpdatesAsync(GetUpdatesRequest request);
}
