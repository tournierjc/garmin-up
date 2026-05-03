using Newtonsoft.Json;

namespace Networking.Connect.Dto.DeviceService;

public class RegisterDeviceResponse
{
	public uint UnitId { get; set; }

	public string? DisplayName { get; set; }

	[JsonProperty("wifiSetup")]
	public bool IsWifiSetUp { get; set; }

	public string? PartNumber { get; set; }

	public string? Description { get; set; }

	public string? SoftwareVersion { get; set; }

	public long RegistrationID { get; set; }

	public string? Consumer { get; set; }

	public string? Secret { get; set; }

	public string? OAuthToken { get; set; }

	public string? OAuthTokenSecret { get; set; }

	[JsonProperty("newRegistration")]
	public bool IsNewRegistration { get; set; }

	public string? DeviceStatus { get; set; }

	[JsonProperty("hybrid")]
	public bool IsHybrid { get; set; }

	public string? ShortName { get; set; }
}
