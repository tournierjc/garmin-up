using Services;

namespace Networking;

public class NetworkingSettings : SettingsBase
{
	public string? CountryCode { get; set; }

	public double? EstimatedDownloadSpeed { get; set; }

	private NetworkingSettings()
	{
	}

	public static NetworkingSettings Get()
	{
		return SettingsBase.Get<NetworkingSettings>() ?? new NetworkingSettings();
	}
}
