using System.Threading.Tasks;
using Networking.Omt.Dto.SoftwareUpdateService;

namespace Networking.Omt;

public interface ISoftwareUpdateService
{
	Task<AllUnitSoftwareUpdatesResponse> GetAllUnitSoftwareUpdatesAsync(bool isUserInteractive);
}
