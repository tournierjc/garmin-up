using System.Threading.Tasks;
using Networking.Connect.Dto.DeviceBackupService;

namespace Networking.Connect;

public interface IDeviceBackupService
{
	Task<bool> GetBackupSummaryAsync();

	Task<DeviceBackup[]> GetCompatibleBackupsAsync();

	Task DeleteBackupAsync(DeviceBackup backup);

	Task RestoreBackupAsync(DeviceBackup backup);
}
