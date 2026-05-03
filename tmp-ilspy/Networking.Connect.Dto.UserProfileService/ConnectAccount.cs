using Newtonsoft.Json;

namespace Networking.Connect.Dto.UserProfileService;

public class ConnectAccount
{
	[JsonProperty("userProfilePk")]
	public long UserId { get; set; }

	[JsonProperty("userName")]
	public string? Username { get; set; }

	[JsonProperty("emailAddress")]
	public string? Email { get; set; }

	[JsonProperty("garminGUID")]
	public string? GarminGuid { get; set; }

	[JsonProperty("phoneNumber")]
	public string? PhoneNumber { get; set; }
}
