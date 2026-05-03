using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DeviceXmlUtil;
using Networking.Auth;
using Networking.Connect;
using Networking.ITServices.Dto;
using Networking.ITServices.Dto.ConnectIq;
using RestSharp;

namespace Networking.ITServices;

internal class AppServices : IAppServices
{
	private record IqUpdateRequest(IqAppRequest[] Apps, string DeviceSKU, string Locale);

	private record IqInstallRequest(IqAppRequest[] Apps, uint UnitId);

	private record IqRemoveRequest(IqAppRequest[] Apps, uint UnitId);

	private readonly ITRestClient _restClient;

	private readonly IAuthDataProvider _auth;

	private readonly UnitId _unitId;

	private readonly XmlDevice _xmlDevice;

	public AppServices(ITRestClient restClient, IAuthDataProvider auth, UnitId unitId, XmlDevice xmlDevice)
	{
		_restClient = restClient;
		_auth = auth;
		_unitId = unitId;
		_xmlDevice = xmlDevice;
	}

	public async Task<IqItemResponse[]> GetConnectIqItemsAsync()
	{
		ConnectAuth auth = _auth.GetConnectAuth(_unitId) ?? throw new ConnectAuthorizationException();
		RestRequest restRequest = new RestRequest($"express/appstore/rest/apps/downloads?unitId={_unitId.Id}&locale={CultureInfo.CurrentCulture.Name}&sku={_xmlDevice.Model.PartNumber}");
		restRequest.Authenticator = ConnectTokenProvider.GetAuthenticatorForAccessToken(auth);
		return (await _restClient.ExecuteAsync<IqItemResponse[]>(restRequest)).Data;
	}

	public async Task<IqItemResponse[]> GetConnectIqUpdatesAsync(IqAppRequest[] iqApps)
	{
		ConnectAuth auth = _auth.GetConnectAuth(_unitId) ?? throw new ConnectAuthorizationException();
		RestRequest restRequest = new RestRequest("express/appstore/rest/apps/updates", Method.Post);
		restRequest.Authenticator = ConnectTokenProvider.GetAuthenticatorForAccessToken(auth);
		restRequest.AddJsonBody(new IqUpdateRequest(iqApps, _xmlDevice.Model.PartNumber, CultureInfo.CurrentCulture.Name));
		return (await _restClient.ExecuteAsync<IqItemResponse[]>(restRequest)).Data;
	}

	public async Task<IqInstallResponse[]> InstallAppsAsync(IqAppRequest[] iqApps)
	{
		ConnectAuth auth = _auth.GetConnectAuth(_unitId) ?? throw new ConnectAuthorizationException();
		RestRequest restRequest = new RestRequest("express/appstore/rest/apps/installApps", Method.Post);
		restRequest.Authenticator = ConnectTokenProvider.GetAuthenticatorForAccessToken(auth);
		restRequest.AddJsonBody(new IqInstallRequest(iqApps, _unitId.Id));
		return (await _restClient.ExecuteAsync<IqInstallResponse[]>(restRequest)).Data;
	}

	public async Task<IqRemoveResponse[]> RemoveInstalledAppsAsync(params Guid[] iqApps)
	{
		ConnectAuth auth = _auth.GetConnectAuth(_unitId) ?? throw new ConnectAuthorizationException();
		RestRequest restRequest = new RestRequest("express/appstore/rest/apps/removeInstalledApps", Method.Post);
		restRequest.Authenticator = ConnectTokenProvider.GetAuthenticatorForAccessToken(auth);
		restRequest.AddJsonBody(new IqRemoveRequest(iqApps.Select((Guid i) => new IqAppRequest(i, 0u)).ToArray(), _unitId.Id));
		return (await _restClient.ExecuteAsync<IqRemoveResponse[]>(restRequest)).Data;
	}

	public async Task<ConnectIqAppImage[]> GetAppStoreImages(params Guid[] iqApps)
	{
		if (iqApps.Length == 0)
		{
			return Array.Empty<ConnectIqAppImage>();
		}
		try
		{
			ConnectAuth auth = _auth.GetConnectAuth(_unitId) ?? throw new ConnectAuthorizationException();
			RestRequest restRequest = new RestRequest("express/appstore/rest/apps/appIconUrl", Method.Post);
			restRequest.Authenticator = ConnectTokenProvider.GetAuthenticatorForAccessToken(auth);
			restRequest.AddJsonBody(iqApps);
			return (await _restClient.ExecuteAsync<ConnectIqAppImage[]>(restRequest)).Data ?? Array.Empty<ConnectIqAppImage>();
		}
		catch
		{
			return Array.Empty<ConnectIqAppImage>();
		}
	}
}
