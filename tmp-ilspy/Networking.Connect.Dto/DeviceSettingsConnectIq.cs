using Newtonsoft.Json;

namespace Networking.Connect.Dto;

public class DeviceSettingsConnectIq
{
	[JsonProperty("autoUpdate")]
	public required bool AutoUpdate { get; init; }
}
