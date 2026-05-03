using System;
using System.Threading.Tasks;
using Networking.ITServices.Dto;
using Networking.ITServices.Dto.ConnectIq;

namespace Networking.ITServices;

public interface IAppServices
{
	Task<IqItemResponse[]> GetConnectIqItemsAsync();

	Task<IqItemResponse[]> GetConnectIqUpdatesAsync(IqAppRequest[] iqApps);

	Task<IqInstallResponse[]> InstallAppsAsync(IqAppRequest[] iqApps);

	Task<IqRemoveResponse[]> RemoveInstalledAppsAsync(params Guid[] iqApps);

	Task<ConnectIqAppImage[]> GetAppStoreImages(params Guid[] iqApps);
}
