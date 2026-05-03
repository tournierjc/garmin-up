using System;
using System.Net;
using System.Threading.Tasks;
using DeviceXmlUtil;
using Networking.Connect.Dto;
using Networking.Connect.Dto.DeviceService;
using Networking.Connect.Dto.UserProfileService;
using RestSharp;

namespace Networking.Connect;

internal sealed class DeviceService : IDeviceService
{
	private readonly ConnectRestClient _restClient;

	private readonly UnitId _unitId;

	private readonly Func<XmlDevice> _xmlDeviceFunc;

	private readonly ConnectAccount? _connectAccount;

	public DeviceService(ConnectRestClient restClient, UnitId unitId, Func<XmlDevice> xmlDeviceFunc, ConnectAccount? connectAccount = null)
	{
		_restClient = restClient;
		_unitId = unitId;
		_xmlDeviceFunc = xmlDeviceFunc;
		_connectAccount = connectAccount;
	}

	public async Task<string> GetConnectNicknameAsync()
	{
		RestRequest request = new RestRequest($"device-service/deviceregistration/device/name/{_unitId.Id}");
		return (await _restClient.ExecuteAsync(request)).Content;
	}

	public async Task<bool> SetConnectNicknameAsync(string nickname)
	{
		RestRequest request = new RestRequest($"device-service/deviceregistration/device/{_unitId.Id}", Method.Put);
		request.AddJsonBody(new
		{
			displayName = nickname
		});
		bool.TryParse((await _restClient.ExecuteAsync(request)).Content, out var result);
		return result;
	}

	public async Task<bool> SetWifiSetupFlagAsync()
	{
		RestRequest request = new RestRequest($"device-service/deviceregistration/device/{_unitId.Id}", Method.Put);
		request.AddJsonBody(new
		{
			wifiSetup = true
		});
		bool.TryParse((await _restClient.ExecuteAsync(request)).Content, out var result);
		return result;
	}

	public async Task<bool> RemoveAppFromConnectQueue(long messageId)
	{
		RestRequest request = new RestRequest($"device-service/devicemessage/message/{messageId}", Method.Delete);
		bool.TryParse((await _restClient.ExecuteAsync(request)).Content, out var result);
		return result;
	}

	public async Task<RegisterDeviceResponse> RegisterDeviceWithConnectAsync(RegistrationOverride option)
	{
		XmlDevice xmlDevice = _xmlDeviceFunc();
		RestRequest request = new RestRequest("device-service/deviceregistration/device", Method.Post);
		request.AddHeader("Content-Type", "application/xml");
		request.AddXmlBody(xmlDevice.ToString());
		if (option != RegistrationOverride.None)
		{
			request.AddHeader("Override", option.ToString("d"));
		}
		try
		{
			return (await _restClient.ExecuteAsync<RegisterDeviceResponse>(request)).Data;
		}
		catch (RestRequestException ex)
		{
			DeviceRegistrationError? deviceRegistrationError = ex.Response.StatusCode switch
			{
				HttpStatusCode.BadRequest => DeviceRegistrationError.InvalidXmlOrNoUDIEntry, 
				HttpStatusCode.Forbidden => DeviceRegistrationError.DeviceRegistrationNotAllowed, 
				HttpStatusCode.Conflict => DeviceRegistrationError.DeviceAssociatedWithOtherUser, 
				_ => null, 
			};
			if (deviceRegistrationError.HasValue)
			{
				throw new DeviceRegistrationException(deviceRegistrationError.Value);
			}
			throw;
		}
	}

	public async Task SetDeviceToActiveAsync()
	{
		RestRequest request = new RestRequest($"device-service/deviceregistration/device/status/{_unitId.Id}", Method.Put);
		request.AddJsonBody(new
		{
			deviceStatus = "active"
		});
		await _restClient.ExecuteAsync(request);
	}

	public async Task<PrimaryTrainingDeviceResponse> GetPrimaryTrainingAndRegisteredDevicesAsync(long userId)
	{
		RestRequest request = new RestRequest("express-gateway/device-info/primary-training-device");
		request.AddHeader("user_id", userId);
		return (await _restClient.ExecuteAsync<PrimaryTrainingDeviceResponse>(request)).Data;
	}

	public async Task<DeviceSettings> GetDeviceSettingsAsync()
	{
		RestRequest request = new RestRequest($"device-service/deviceservice/device-info/settings/{_unitId.Id}");
		return (await _restClient.ExecuteAsync<DeviceSettings>(request)).Data;
	}

	public async Task SetDeviceSettingsAsync(DeviceSettings deviceSettings)
	{
		RestRequest request = new RestRequest($"device-service/deviceservice/device-info/settings/{_unitId.Id}", Method.Put);
		request.AddJsonBody(deviceSettings);
		await _restClient.ExecuteAsync(request);
	}

	public async Task SetDeviceSettingLanguageAsync(DeviceSettingsLanguage language)
	{
		RestRequest request = new RestRequest($"device-service/deviceservice/device-info/{_unitId.Id}/setting/language", Method.Put);
		request.AddStringBody(language.Id.ToString(), DataFormat.Json);
		await _restClient.ExecuteAsync(request);
	}

	public async Task<DeviceSettingsConnectIq> GetConnectIqDeviceSettingsAsync()
	{
		RestRequest request = new RestRequest($"device-service/deviceservice/device-info/{_unitId.Id}/setting/connect-iq");
		return (await _restClient.ExecuteAsync<DeviceSettingsConnectIq>(request)).Data;
	}

	public async Task<DeviceSettingsConnectIq> SetConnectIqDeviceSettingsAsync(bool shouldAutoUpdateCiqApps)
	{
		RestRequest request = new RestRequest($"device-service/deviceservice/device-info/{_unitId.Id}/setting/connect-iq", Method.Put);
		request.AddJsonBody(new
		{
			autoUpdate = shouldAutoUpdateCiqApps
		});
		return (await _restClient.ExecuteAsync<DeviceSettingsConnectIq>(request)).Data;
	}

	public async Task<bool> AckDownloadAsync(long messageId)
	{
		RestRequest request = new RestRequest($"device-service/devicemessage/message/{messageId}?status=received", Method.Post);
		request.AddHeader("X-HTTP-Method-Override", "PUT");
		bool.TryParse((await _restClient.ExecuteAsync(request)).Content, out var result);
		return result;
	}

	public async Task<bool> GetDeviceTrueUpStatusAsync()
	{
		if (_connectAccount == null)
		{
			throw new InvalidOperationException("_connectAccount");
		}
		RestRequest request = new RestRequest("device-service/deviceservice/multiple-activity-trackers");
		request.AddHeader("user_id", _connectAccount.UserId);
		bool.TryParse((await _restClient.ExecuteAsync(request)).Content, out var result);
		return result;
	}

	public async Task<RestResponse> GetDeviceMessagesAsync()
	{
		RestRequest request = new RestRequest($"device-service/devicemessage/messages?device_id={_unitId.Id}");
		return await _restClient.ExecuteAsync(request);
	}

	public async Task<DeviceInfo> GetDeviceInfoAsync()
	{
		RestRequest request = new RestRequest($"device-service/deviceservice/device-info/{_unitId.Id}");
		return (await _restClient.ExecuteAsync<DeviceInfo>(request)).Data;
	}

	public async Task<bool> SetShortNameAsync(string name)
	{
		RestRequest request = new RestRequest($"device-service/deviceservice/device-info/{_unitId.Id}/setting/shortName", Method.Put);
		request.AddBody(name, ContentType.Json);
		bool.TryParse((await _restClient.ExecuteAsync(request)).Content, out var result);
		return result;
	}

	public async Task SendDeviceXmlToConnectAsync()
	{
		RestRequest request = new RestRequest("device-service/softwareupdate/device", Method.Post);
		request.AddXmlBody(_xmlDeviceFunc().ToString());
		await _restClient.ExecuteAsync(request);
	}

	public async Task SetPrimaryActivityTrackerAsync()
	{
		RestRequest request = new RestRequest($"device-service/deviceservice/device-info/{_unitId.Id}/primary-activity-tracker", Method.Put);
		request.AddBody("true", ContentType.Json);
		await _restClient.ExecuteAsync(request);
	}
}
