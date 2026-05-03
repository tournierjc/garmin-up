using System.Threading.Tasks;
using Networking.Omt.Dto.ApplicationService;

namespace Networking.Omt;

public interface IApplicationService
{
	Task<ApplicationUpdateResponse> GetApplicationUpdateAsync(ApplicationUpdateRequest appUpdateRequest);

	Task SendErrorReport(string referenceCode, byte[] data, string message = "");

	Task<NotificationMessage[]> GetNotificationsAsync(UnitId[] unitIds, bool skipCache = false);
}
