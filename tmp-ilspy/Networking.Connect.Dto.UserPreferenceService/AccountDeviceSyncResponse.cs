using Newtonsoft.Json;

namespace Networking.Connect.Dto.UserPreferenceService;

public class AccountDeviceSyncResponse
{
	[JsonProperty("key")]
	public string? Key { get; set; }

	[JsonProperty("value")]
	public bool Value { get; set; }
}
