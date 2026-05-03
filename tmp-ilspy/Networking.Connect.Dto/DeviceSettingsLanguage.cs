using Newtonsoft.Json;

namespace Networking.Connect.Dto;

public class DeviceSettingsLanguage
{
	[JsonProperty("id")]
	public required int Id { get; init; }

	[JsonProperty("name")]
	public required string Name { get; init; }
}
