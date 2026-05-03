using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Networking.Omt.Dto.ApplicationService;
using Networking.Omt.Dto.Common;
using RestSharp;

namespace Networking.Omt;

internal sealed class ApplicationService : IApplicationService
{
	private record NotificationsRequest(BasicUnitInfo[] Units)
	{
		public object ClientInfo { get; } = new
		{
			ClientType = Constants.AppName,
			LocaleCode = CultureInfo.CurrentCulture.Name,
			OperatingSystemType = Constants.OSPlatform.ToString(),
			OperatingSystemVersion = Environment.OSVersion.Version.ToString()
		};
	}

	private record NotificationsResponse(NotificationMessage[] Messages);

	private record EventClientInfo(string ApplicationVersion, string ComputerGuid)
	{
		public string ApplicationName => Constants.AppName;

		public string Component => Constants.AppName;

		public string LocaleCode => CultureInfo.CurrentCulture.Name;

		public string OperatingSystemType => Constants.OSPlatform.ToString();

		public string OperatingSystemVersion => Environment.OSVersion.Version.ToString();
	}

	private record ReportErrorEvent(EventClientInfo EventClientInfo, DateTime TimeStamp, string Message, byte[] ErrorData, string ReferenceCode);

	private readonly OmtRestClient _restClient;

	private readonly InstallationGuid _installationGuid;

	private readonly AppVersion _appVersion;

	private readonly ICache _cache;

	public ApplicationService(OmtRestClient restClient, InstallationGuid installationGuid, AppVersion appVersion, ICache cache)
	{
		_restClient = restClient;
		_installationGuid = installationGuid;
		_appVersion = appVersion;
		_cache = cache;
	}

	public async Task<ApplicationUpdateResponse> GetApplicationUpdateAsync(ApplicationUpdateRequest appUpdateRequest)
	{
		RestRequest request = new RestRequest("/Rce/ProtobufApi/ApplicationService/GetApplicationUpdate", Method.Post);
		request.AddJsonBody(appUpdateRequest);
		return (await _restClient.ExecuteAsync<ApplicationUpdateResponse>(request)).Data;
	}

	public async Task SendErrorReport(string referenceCode, byte[] data, string message)
	{
		RestRequest request = new RestRequest("/Rce/ProtobufApi/ApplicationService/ReportErrorEvent", Method.Post);
		EventClientInfo eventClientInfo = new EventClientInfo(_appVersion.Version.ToString(), _installationGuid.Guid.ToString());
		request.AddJsonBody(new ReportErrorEvent(eventClientInfo, DateTime.UtcNow, message, data, referenceCode));
		await _restClient.ExecuteAsync(request);
	}

	public async Task<NotificationMessage[]> GetNotificationsAsync(UnitId[] unitIds, bool skipCache)
	{
		if (!skipCache)
		{
			NotificationMessage[] array = _cache.Get<NotificationMessage[]>(new byte[1], TimeSpan.FromDays(3.0));
			if (array != null)
			{
				return array;
			}
		}
		RestRequest request = new RestRequest("/Rce/ProtobufApi/ApplicationService/GetNotifications", Method.Post);
		request.AddJsonBody(new NotificationsRequest(unitIds.Select((UnitId i) => new BasicUnitInfo(i)).ToArray()));
		try
		{
			NotificationMessage[] array2 = (await _restClient.ExecuteAsync<NotificationsResponse>(request)).Data?.Messages ?? Array.Empty<NotificationMessage>();
			_cache.Set(new byte[1], array2);
			return array2;
		}
		catch (RestRequestException)
		{
			return Array.Empty<NotificationMessage>();
		}
	}
}
