using System.Threading.Tasks;
using Networking.Connect.Dto;
using Networking.Connect.Dto.DeviceService;
using RestSharp;

namespace Networking.Connect;

public interface IDeviceService
{
	Task<string> GetConnectNicknameAsync();

	Task<bool> SetConnectNicknameAsync(string nickname);

	Task<bool> SetWifiSetupFlagAsync();

	Task<bool> RemoveAppFromConnectQueue(long messageId);

	Task<RegisterDeviceResponse> RegisterDeviceWithConnectAsync(RegistrationOverride option = RegistrationOverride.None);

	Task SetDeviceToActiveAsync();

	Task<PrimaryTrainingDeviceResponse> GetPrimaryTrainingAndRegisteredDevicesAsync(long userId);

	Task<DeviceSettings> GetDeviceSettingsAsync();

	Task SetDeviceSettingsAsync(DeviceSettings deviceSettings);

	Task SetDeviceSettingLanguageAsync(DeviceSettingsLanguage language);

	Task<DeviceSettingsConnectIq> GetConnectIqDeviceSettingsAsync();

	Task<DeviceSettingsConnectIq> SetConnectIqDeviceSettingsAsync(bool shouldAutoUpdateCiqApps);

	Task<bool> AckDownloadAsync(long messageId);

	Task<bool> GetDeviceTrueUpStatusAsync();

	Task<RestResponse> GetDeviceMessagesAsync();

	Task<DeviceInfo> GetDeviceInfoAsync();

	Task<bool> SetShortNameAsync(string name);

	Task SendDeviceXmlToConnectAsync();

	Task SetPrimaryActivityTrackerAsync();
}
