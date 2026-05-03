using System.Threading.Tasks;
using Networking.Connect.Dto.DeviceBackupService;
using RestSharp;

namespace Networking.Connect;

internal sealed class DeviceBackupService : IDeviceBackupService
{
	private record BackupSummaryResponse(bool UserHasCompleteBackup);

	private readonly ConnectRestClient _restClient;

	private readonly uint _unitId;

	public DeviceBackupService(ConnectRestClient restClient, UnitId unitId)
	{
		_restClient = restClient;
		_unitId = unitId.Id;
	}

	public async Task<bool> GetBackupSummaryAsync()
	{
		RestRequest request = new RestRequest("devicebackup-service/backup/summary");
		return (await _restClient.ExecuteAsync<BackupSummaryResponse>(request)).Data.UserHasCompleteBackup;
	}

	public async Task<DeviceBackup[]> GetCompatibleBackupsAsync()
	{
		RestRequest request = new RestRequest($"devicebackup-service/backup/{_unitId}");
		return (await _restClient.ExecuteAsync<DeviceBackup[]>(request)).Data;
	}

	public async Task DeleteBackupAsync(DeviceBackup backup)
	{
		RestRequest request = new RestRequest($"devicebackup-service/backup/{backup.DeviceId}", Method.Delete);
		await _restClient.ExecuteAsync(request);
	}

	public async Task RestoreBackupAsync(DeviceBackup backup)
	{
		RestRequest request = new RestRequest($"devicebackup-service/backup/restoreDevice/{_unitId}/{backup.DeviceBackupId}", Method.Post);
		await _restClient.ExecuteAsync(request);
	}
}
